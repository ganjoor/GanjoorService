using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.PublicExport;
using RMuseum.Utils.PublicDataImport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation: importing people, places, couplet tags and literary devices
    /// from a public data export tree (see GanjoorService-PublicDataExport.cs for the file layout)
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        /// <summary>
        /// state of one import run: which ids exist locally, how ids of the export map to local ids
        /// (literary devices, slots and concepts are matched with what is already in the database, so
        /// their ids can differ) and what was added
        /// </summary>
        private class PublicImportContext
        {
            public HashSet<int> LocationIds = new HashSet<int>();
            public HashSet<int> PersonIds = new HashSet<int>();
            public Dictionary<int, int> DeviceIds = new Dictionary<int, int>();
            public Dictionary<int, int> SlotIds = new Dictionary<int, int>();
            public Dictionary<int, int> ConceptIds = new Dictionary<int, int>();

            /// <summary>
            /// metre ids of the export => local metre ids
            /// </summary>
            public Dictionary<int, int> MetreIds = new Dictionary<int, int>();
            public int Metres;

            /// <summary>
            /// the database had no tags of this kind when the import started, so poems that already exist
            /// get their tags from the export too (otherwise only poems imported by this run do)
            /// </summary>
            public bool BackfillGeoTags;
            public bool BackfillLiteraryTags;

            /// <summary>
            /// the export is older than schema 4: literary couplet numbers are 1 based and 0 means the whole poem,
            /// so subtracting 1 gives the current convention (0 based, -1 whole poem / not located)
            /// </summary>
            public bool LegacyLiteraryCoupletNumbers;

            public int Locations, People, Relations, Affiliations, Evidences;
            public int Devices, Slots, Concepts, Sources;
            public int GeoTags, PersonTags, LiteraryTags, SkippedTags;

            public string Summary()
            {
                return $"metres: {Metres}; locations: {Locations}, people: {People}, relations: {Relations}, affiliations: {Affiliations}, evidences: {Evidences}; " +
                       $"literary devices: {Devices}, slots: {Slots}, concepts: {Concepts}, sources: {Sources}; " +
                       $"tags: places/dates {GeoTags}, people {PersonTags}, literary {LiteraryTags}, skipped {SkippedTags}";
            }
        }

        private static async Task<List<T>> ReadPublicDataListAsync<T>(IPublicDataSource source, string path)
        {
            string json = await source.ReadTextAsync(path);
            if (json == null)
                return new List<T>(); // an export made before this kind of data was added
            return JsonSerializer.Deserialize<List<T>>(json, _importJsonOptions) ?? new List<T>();
        }

        /// <summary>
        /// inserts entities whose ids come from the export. These tables have identity columns, which refuse explicit
        /// values unless IDENTITY_INSERT is on (only one table at a time, hence one call per table)
        /// </summary>
        private static async Task InsertWithExplicitIdsAsync<T>(RMuseumDbContext context, List<T> items) where T : class
        {
            if (items.Count == 0)
                return;
            var entityType = context.Model.FindEntityType(typeof(T));
            string table = entityType.GetTableName();
            string schema = entityType.GetSchema() ?? "dbo";
            bool sqlServer = context.Database.ProviderName != null && context.Database.ProviderName.Contains("SqlServer");

            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                context.ChangeTracker.Clear();
                if (!sqlServer)
                {
                    context.AddRange(items);
                    await context.SaveChangesAsync();
                    return;
                }
                using (var transaction = await context.Database.BeginTransactionAsync())
                {
                    await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{schema}].[{table}] ON");
                    context.AddRange(items);
                    await context.SaveChangesAsync();
                    await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{schema}].[{table}] OFF");
                    await transaction.CommitAsync();
                }
            });
            context.ChangeTracker.Clear();
        }

        /// <summary>
        /// places, people and literary devices/concepts/sources: everything the tags refer to, so it has to be in
        /// place before the poems are imported. Existing rows are never touched: places and people are matched by id,
        /// literary devices by key (or name) and concepts by name, because a database may have its own core devices
        /// with other ids
        /// </summary>
        private async Task ImportSharedTaggingDataFromPublicData(RMuseumDbContext context, IPublicDataSource source, PublicImportContext pic)
        {
            // ---- metres: poems and their sections refer to them by foreign key, and nothing else creates them in an empty
            // database. Matched by rhythm (the natural key), so a database with metres of its own keeps them.
            var metreDtos = await ReadPublicDataListAsync<MetrePublicDto>(source, "metres.json");
            if (metreDtos.Count > 0)
            {
                var localMetres = await context.GanjoorMetres.AsNoTracking().Select(m => new { m.Id, m.Rhythm }).ToListAsync();
                var metreByRhythm = new Dictionary<string, int>();
                foreach (var m in localMetres.Where(m => !string.IsNullOrEmpty(m.Rhythm)))
                    if (!metreByRhythm.ContainsKey(m.Rhythm))
                        metreByRhythm[m.Rhythm] = m.Id;
                var usedMetreIds = new HashSet<int>(localMetres.Select(m => m.Id));
                int nextMetreId = Math.Max(usedMetreIds.Count == 0 ? 0 : usedMetreIds.Max(), metreDtos.Max(m => m.Id)) + 1;
                var newMetres = new List<GanjoorMetre>();
                foreach (var dto in metreDtos)
                {
                    if (!string.IsNullOrEmpty(dto.Rhythm) && metreByRhythm.TryGetValue(dto.Rhythm, out int sameRhythmId))
                    {
                        pic.MetreIds[dto.Id] = sameRhythmId;
                        continue;
                    }
                    int metreId = usedMetreIds.Contains(dto.Id) ? nextMetreId++ : dto.Id;
                    usedMetreIds.Add(metreId);
                    if (!string.IsNullOrEmpty(dto.Rhythm))
                        metreByRhythm[dto.Rhythm] = metreId;
                    pic.MetreIds[dto.Id] = metreId;
                    newMetres.Add(new GanjoorMetre
                    {
                        Id = metreId,
                        UrlSlug = dto.UrlSlug,
                        Rhythm = dto.Rhythm,
                        Name = dto.Name,
                        Description = dto.Description,
                        VerseCount = 0, // a statistic, filled by the stats job
                    });
                }
                await InsertWithExplicitIdsAsync(context, newMetres);
                pic.Metres = newMetres.Count;
            }

            // ---- places
            var locationDtos = await ReadPublicDataListAsync<LocationPublicDto>(source, "locations.json");
            var existingLocationIds = new HashSet<int>(await context.GanjoorGeoLocations.AsNoTracking().Select(l => l.Id).ToListAsync());
            var newLocations = locationDtos.Where(l => !existingLocationIds.Contains(l.Id)).Select(l => new GanjoorGeoLocation
            {
                Id = l.Id,
                Name = l.Name,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                MachineGenerated = l.MachineGenerated,
            }).ToList();
            await InsertWithExplicitIdsAsync(context, newLocations);
            pic.Locations = newLocations.Count;
            pic.LocationIds = new HashSet<int>(existingLocationIds.Concat(newLocations.Select(l => l.Id)));

            // ---- people
            var personDtos = await ReadPublicDataListAsync<PersonPublicDto>(source, "people.json");
            var existingPersonIds = new HashSet<int>(await context.GanjoorRelatedPersons.AsNoTracking().Select(p => p.Id).ToListAsync());
            var newPeople = personDtos.Where(p => !existingPersonIds.Contains(p.Id)).Select(p => new GanjoorRelatedPerson
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Aliases = p.Aliases,
                WikiUrl = p.WikiUrl,
                Gender = Enum.TryParse<PersonGender>(p.Gender, out var gender) ? gender : default(PersonGender),
                Importance = Enum.TryParse<PersonImportance>(p.Importance, out var importance) ? importance : default(PersonImportance),
                BirthYearInLHijri = p.BirthYearInLHijri,
                ValidBirthDate = p.ValidBirthDate,
                DeathYearInLHijri = p.DeathYearInLHijri,
                ValidDeathDate = p.ValidDeathDate,
                BirthLocationId = p.BirthLocationId != null && pic.LocationIds.Contains(p.BirthLocationId.Value) ? p.BirthLocationId : null,
                DeathLocationId = p.DeathLocationId != null && pic.LocationIds.Contains(p.DeathLocationId.Value) ? p.DeathLocationId : null,
                FamilyTreeCaption = p.FamilyTreeCaption,
                MachineGenerated = p.MachineGenerated,
            }).ToList();
            await InsertWithExplicitIdsAsync(context, newPeople);
            pic.People = newPeople.Count;
            pic.PersonIds = new HashSet<int>(existingPersonIds.Concat(newPeople.Select(p => p.Id)));

            // ---- literary devices and their slots
            var deviceDtos = await ReadPublicDataListAsync<LiteraryDevicePublicDto>(source, "literary-devices.json");
            if (deviceDtos.Count > 0)
            {
                var localDevices = await context.LiteraryDevices.AsNoTracking().Include(d => d.Slots).ToListAsync();
                var usedDeviceIds = new HashSet<int>(localDevices.Select(d => d.Id));
                var usedSlotIds = new HashSet<int>(await context.LiteraryDeviceSlots.AsNoTracking().Select(s => s.Id).ToListAsync());
                int nextDeviceId = Math.Max(usedDeviceIds.Count == 0 ? 0 : usedDeviceIds.Max(), deviceDtos.Max(d => d.Id)) + 1;
                int nextSlotId = Math.Max(usedSlotIds.Count == 0 ? 0 : usedSlotIds.Max(), deviceDtos.SelectMany(d => d.Slots).Select(s => s.Id).DefaultIfEmpty(0).Max()) + 1;

                var newDevices = new List<LiteraryDevice>();
                var newSlots = new List<LiteraryDeviceSlot>();
                foreach (var dto in deviceDtos)
                {
                    var local = (!string.IsNullOrEmpty(dto.Key) ? localDevices.FirstOrDefault(d => d.Key == dto.Key) : null)
                                ?? localDevices.FirstOrDefault(d => d.Name == dto.Name);
                    int deviceId;
                    var localSlots = new List<LiteraryDeviceSlot>();
                    if (local != null)
                    {
                        deviceId = local.Id;
                        localSlots = local.Slots.ToList();
                    }
                    else
                    {
                        deviceId = usedDeviceIds.Contains(dto.Id) ? nextDeviceId++ : dto.Id;
                        usedDeviceIds.Add(deviceId);
                        newDevices.Add(new LiteraryDevice
                        {
                            Id = deviceId,
                            Key = dto.Key,
                            Name = dto.Name,
                            Category = Enum.TryParse<LiteraryDeviceCategory>(dto.Category, out var category) ? category : LiteraryDeviceCategory.Other,
                            Definition = dto.Definition,
                            Example = dto.Example,
                            IsCore = dto.IsCore,
                            Status = LiteraryEntityStatus.Approved,
                            CreateDate = DateTime.Now,
                        });
                    }
                    pic.DeviceIds[dto.Id] = deviceId;

                    foreach (var slotDto in dto.Slots)
                    {
                        var localSlot = (!string.IsNullOrEmpty(slotDto.Key) ? localSlots.FirstOrDefault(s => s.Key == slotDto.Key) : null)
                                        ?? localSlots.FirstOrDefault(s => s.Name == slotDto.Name);
                        if (localSlot != null)
                        {
                            pic.SlotIds[slotDto.Id] = localSlot.Id;
                            continue;
                        }
                        int slotId = usedSlotIds.Contains(slotDto.Id) ? nextSlotId++ : slotDto.Id;
                        usedSlotIds.Add(slotId);
                        newSlots.Add(new LiteraryDeviceSlot
                        {
                            Id = slotId,
                            DeviceId = deviceId,
                            Key = slotDto.Key,
                            Name = slotDto.Name,
                            Kind = Enum.TryParse<LiteraryDeviceSlotKind>(slotDto.Kind, out var kind) ? kind : LiteraryDeviceSlotKind.Text,
                            Required = slotDto.Required,
                            AllowMultiple = slotDto.AllowMultiple,
                            Order = slotDto.Order,
                        });
                        pic.SlotIds[slotDto.Id] = slotId;
                    }
                }
                await InsertWithExplicitIdsAsync(context, newDevices);
                await InsertWithExplicitIdsAsync(context, newSlots);
                pic.Devices = newDevices.Count;
                pic.Slots = newSlots.Count;
            }

            // ---- literary concepts (matched by normalized name)
            var conceptDtos = await ReadPublicDataListAsync<LiteraryConceptPublicDto>(source, "literary-concepts.json");
            if (conceptDtos.Count > 0)
            {
                var localConcepts = await context.LiteraryConcepts.AsNoTracking().Select(c => new { c.Id, c.Name }).ToListAsync();
                var byName = new Dictionary<string, int>();
                foreach (var c in localConcepts)
                {
                    string key = _NormalizeConceptName(c.Name);
                    if (!byName.ContainsKey(key))
                        byName[key] = c.Id;
                }
                var usedConceptIds = new HashSet<int>(localConcepts.Select(c => c.Id));
                int nextConceptId = Math.Max(usedConceptIds.Count == 0 ? 0 : usedConceptIds.Max(), conceptDtos.Max(c => c.Id)) + 1;

                var newConcepts = new List<LiteraryConcept>();
                foreach (var dto in conceptDtos)
                {
                    string name = _NormalizeConceptName(dto.Name);
                    if (byName.TryGetValue(name, out int sameNameId))
                    {
                        pic.ConceptIds[dto.Id] = sameNameId;
                        continue;
                    }
                    int conceptId = usedConceptIds.Contains(dto.Id) ? nextConceptId++ : dto.Id;
                    usedConceptIds.Add(conceptId);
                    byName[name] = conceptId;
                    pic.ConceptIds[dto.Id] = conceptId;
                    newConcepts.Add(new LiteraryConcept
                    {
                        Id = conceptId,
                        Name = name,
                        Aliases = dto.Aliases,
                        Status = LiteraryEntityStatus.Approved,
                        CreateDate = DateTime.Now,
                    });
                }
                await InsertWithExplicitIdsAsync(context, newConcepts);
                pic.Concepts = newConcepts.Count;

                // parents are linked afterwards: a parent can have a higher id than its child
                var parentLinks = new List<Tuple<int, int>>();
                var newConceptIds = new HashSet<int>(newConcepts.Select(n => n.Id));
                foreach (var dto in conceptDtos.Where(c => c.ParentId != null))
                {
                    if (newConceptIds.Contains(pic.ConceptIds[dto.Id]) && pic.ConceptIds.TryGetValue(dto.ParentId.Value, out int parentId) && parentId != pic.ConceptIds[dto.Id])
                        parentLinks.Add(Tuple.Create(pic.ConceptIds[dto.Id], parentId));
                }
                foreach (var chunk in parentLinks.Select((link, index) => new { link, index }).GroupBy(x => x.index / 500).Select(g => g.Select(x => x.link).ToList()))
                {
                    var ids = chunk.Select(l => l.Item1).ToList();
                    var tracked = await context.LiteraryConcepts.Where(c => ids.Contains(c.Id)).ToListAsync();
                    foreach (var concept in tracked)
                        concept.ParentId = chunk.First(l => l.Item1 == concept.Id).Item2;
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear();
                }
            }

            // ---- sources (origin and license of imported literary tags)
            var sourceDtos = await ReadPublicDataListAsync<LiteraryTagSourcePublicDto>(source, "literary-sources.json");
            if (sourceDtos.Count > 0)
            {
                var existingKeys = new HashSet<string>(await context.LiteraryTagSources.AsNoTracking().Select(s => s.Key).ToListAsync());
                foreach (var dto in sourceDtos.Where(s => !string.IsNullOrEmpty(s.Key) && !existingKeys.Contains(s.Key)))
                {
                    context.LiteraryTagSources.Add(new LiteraryTagSource
                    {
                        Key = dto.Key,
                        Name = dto.Name,
                        Title = dto.Title,
                        Authors = dto.Authors,
                        Url = dto.Url,
                        Description = dto.Description,
                        License = dto.License,
                        LicenseText = dto.LicenseText,
                        UpdateDate = DateTime.Now,
                    });
                    pic.Sources++;
                }
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
            }
        }

        /// <summary>
        /// local id of the metre a poem refers to. The metre is normally already known from metres.json; for an export
        /// without that file (or a metre missing in it) it is created from the poem's own reference
        /// </summary>
        private async Task<int?> EnsurePublicDataMetreAsync(RMuseumDbContext context, PublicImportContext pic, MetreRefDto metre)
        {
            if (metre == null)
                return null;
            if (pic.MetreIds.TryGetValue(metre.Id, out int known))
                return known;
            int? existing = string.IsNullOrEmpty(metre.Rhythm) ? (int?)null
                : await context.GanjoorMetres.AsNoTracking().Where(m => m.Rhythm == metre.Rhythm).Select(m => (int?)m.Id).FirstOrDefaultAsync();
            if (existing == null)
            {
                var created = new GanjoorMetre { Rhythm = metre.Rhythm, Name = metre.Name, VerseCount = 0 };
                context.GanjoorMetres.Add(created);
                await context.SaveChangesAsync();
                existing = created.Id;
            }
            pic.MetreIds[metre.Id] = existing.Value;
            return existing.Value;
        }

        /// <summary>
        /// place/date/person tags and literary tags of one poem. A poem that already has tags of a kind keeps them
        /// untouched; poems that were already in the database before this run are only filled in when the whole
        /// database had no tags of that kind when the run started (see PublicImportContext.BackfillGeoTags)
        /// </summary>
        private async Task ImportPoemTagsFromPublicData(RMuseumDbContext context, int poemId, PoemPublicDto poemDto, PublicImportContext pic, bool isNewPoem)
        {
            if (poemDto.Tags != null && poemDto.Tags.Count > 0 && (isNewPoem || pic.BackfillGeoTags)
                && !await context.PoemGeoDateTags.AnyAsync(t => t.PoemId == poemId))
            {
                int added = 0;
                foreach (var t in poemDto.Tags)
                {
                    if (t.LocationId == null && t.PersonId == null)
                        continue;
                    if ((t.LocationId != null && !pic.LocationIds.Contains(t.LocationId.Value)) || (t.PersonId != null && !pic.PersonIds.Contains(t.PersonId.Value)))
                    {
                        pic.SkippedTags++;
                        continue;
                    }
                    var tag = new PoemGeoDateTag
                    {
                        PoemId = poemId,
                        CoupletIndex = t.CoupletIndex,
                        LocationId = t.LocationId,
                        PersonId = t.PersonId,
                        PersonMention = Enum.TryParse<PersonMentionKind>(t.PersonMention, out var mention) ? mention : default(PersonMentionKind),
                        LunarYear = t.LunarYear,
                        LunarMonth = t.LunarMonth,
                        LunarDay = t.LunarDay,
                        VerifiedDate = t.VerifiedDate,
                        IgnoreInCategory = t.IgnoreInCategory,
                        MachineGenerated = t.MachineGenerated,
                        Note = t.Note,
                    };
                    tag.LunarDateTotalNumber = _PrepareLunarDateTotalNumber(tag);
                    context.PoemGeoDateTags.Add(tag);
                    if (t.PersonId != null)
                        pic.PersonTags++;
                    else
                        pic.GeoTags++;
                    added++;
                }
                if (added > 0)
                    await context.SaveChangesAsync();
            }

            if (poemDto.LiteraryTags != null && poemDto.LiteraryTags.Count > 0 && (isNewPoem || pic.BackfillLiteraryTags)
                && !await context.PoemLiteraryTags.AnyAsync(t => t.PoemId == poemId))
            {
                int added = 0;
                foreach (var lt in poemDto.LiteraryTags)
                {
                    bool valid = pic.DeviceIds.TryGetValue(lt.DeviceId, out int deviceId);
                    var parts = new List<PoemLiteraryTagPart>();
                    int order = 0;
                    foreach (var p in lt.Parts ?? new List<LiteraryTagPartPublicDto>())
                    {
                        if (!valid)
                            break;
                        if (!pic.SlotIds.TryGetValue(p.SlotId, out int slotId))
                        {
                            valid = false;
                            break;
                        }
                        int? conceptId = null;
                        if (p.ConceptId != null)
                        {
                            if (!pic.ConceptIds.TryGetValue(p.ConceptId.Value, out int mapped))
                            {
                                valid = false;
                                break;
                            }
                            conceptId = mapped;
                        }
                        parts.Add(new PoemLiteraryTagPart
                        {
                            SlotId = slotId,
                            CoupletIndex = pic.LegacyLiteraryCoupletNumbers ? p.CoupletIndex - 1 : p.CoupletIndex,
                            Misra = p.Misra,
                            WordStart = p.WordStart,
                            WordEnd = p.WordEnd,
                            ConceptId = conceptId,
                            Text = p.Text,
                            Order = order++,
                        });
                    }
                    if (!valid)
                    {
                        pic.SkippedTags++;
                        continue;
                    }
                    var origin = Enum.TryParse<LiteraryTagOrigin>(lt.Origin, out var parsedOrigin) ? parsedOrigin : LiteraryTagOrigin.Editor;
                    context.PoemLiteraryTags.Add(new PoemLiteraryTag
                    {
                        PoemId = poemId,
                        CoupletIndex = pic.LegacyLiteraryCoupletNumbers ? lt.CoupletIndex - 1 : lt.CoupletIndex,
                        DeviceId = deviceId,
                        Uncertain = lt.Uncertain,
                        Note = lt.Note,
                        Origin = origin,
                        OriginRef = origin == LiteraryTagOrigin.AcademicCorpus ? lt.SourceKey : null,
                        CreateDate = DateTime.Now,
                        Parts = parts,
                    });
                    pic.LiteraryTags++;
                    added++;
                }
                if (added > 0)
                    await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// an existing poem is not re-imported, but its tags can still be missing (see ImportPoemTagsFromPublicData)
        /// </summary>
        private async Task BackfillPoemTagsFromPublicData(RMuseumDbContext context, IPublicDataSource source, int poemId, string poemFullUrl, PublicImportContext pic)
        {
            string poemJson = await source.ReadTextAsync($"poets{poemFullUrl}.json");
            if (poemJson == null)
                return;
            var poemDto = JsonSerializer.Deserialize<PoemPublicDto>(poemJson, _importJsonOptions);
            await ImportPoemTagsFromPublicData(context, poemId, poemDto, pic, isNewPoem: false);
            context.ChangeTracker.Clear();
        }

        /// <summary>
        /// family relations and other ties between people, with the couplets that attest them. Runs after the poems
        /// because the evidence couplet text is taken from the imported verses; evidence of poems that were not
        /// imported (e.g. when a single poet was requested) is left out
        /// </summary>
        private async Task ImportPersonRelationsFromPublicData(RMuseumDbContext context, IPublicDataSource source, PublicImportContext pic)
        {
            // ---- kinship
            var relationDtos = await ReadPublicDataListAsync<PersonRelationPublicDto>(source, "person-relations.json");
            var existingRelationIds = new HashSet<int>(await context.GanjoorPersonRelations.AsNoTracking().Select(r => r.Id).ToListAsync());
            var relationsWithEvidence = new HashSet<int>(await context.GanjoorPersonRelationEvidences.AsNoTracking().Select(e => e.RelationId).Distinct().ToListAsync());
            var newRelations = relationDtos
                .Where(r => !existingRelationIds.Contains(r.Id) && pic.PersonIds.Contains(r.Person1Id) && pic.PersonIds.Contains(r.Person2Id)
                            && Enum.TryParse<PersonRelationType>(r.RelationType, out _))
                .Select(r => new GanjoorPersonRelation
                {
                    Id = r.Id,
                    Person1Id = r.Person1Id,
                    Person2Id = r.Person2Id,
                    RelationType = Enum.Parse<PersonRelationType>(r.RelationType),
                    DegreeHint = r.DegreeHint,
                    Note = r.Note,
                }).ToList();
            await InsertWithExplicitIdsAsync(context, newRelations);
            pic.Relations = newRelations.Count;

            var relationEvidence = new List<Tuple<int, PersonEvidencePublicDto>>();
            var validRelationIds = new HashSet<int>(existingRelationIds.Concat(newRelations.Select(r => r.Id)));
            var newRelationIds = new HashSet<int>(newRelations.Select(r => r.Id));
            foreach (var r in relationDtos.Where(r => validRelationIds.Contains(r.Id) && (newRelationIds.Contains(r.Id) || !relationsWithEvidence.Contains(r.Id))))
                foreach (var e in r.Evidence ?? new List<PersonEvidencePublicDto>())
                    relationEvidence.Add(Tuple.Create(r.Id, e));
            pic.Evidences += await ImportPersonEvidencesAsync(context, relationEvidence,
                (relationId, e, text) => new GanjoorPersonRelationEvidence
                {
                    RelationId = relationId,
                    PoemId = e.PoemId,
                    CoupletIndex = e.CoupletIndex,
                    CoupletText = text,
                    MasterCatId = e.MasterCatId,
                    Inferred = e.Inferred == true,
                    DateAdded = DateTime.Now,
                });

            // ---- affiliations
            var affiliationDtos = await ReadPublicDataListAsync<PersonAffiliationPublicDto>(source, "person-affiliations.json");
            var existingAffiliationIds = new HashSet<int>(await context.GanjoorPersonAffiliations.AsNoTracking().Select(a => a.Id).ToListAsync());
            var affiliationsWithEvidence = new HashSet<int>(await context.GanjoorPersonAffiliationEvidences.AsNoTracking().Select(e => e.AffiliationId).Distinct().ToListAsync());
            var newAffiliations = affiliationDtos
                .Where(a => !existingAffiliationIds.Contains(a.Id) && pic.PersonIds.Contains(a.Person1Id) && pic.PersonIds.Contains(a.Person2Id)
                            && Enum.TryParse<PersonAffiliationType>(a.AffiliationType, out _))
                .Select(a => new GanjoorPersonAffiliation
                {
                    Id = a.Id,
                    Person1Id = a.Person1Id,
                    Person2Id = a.Person2Id,
                    AffiliationType = Enum.Parse<PersonAffiliationType>(a.AffiliationType),
                    Note = a.Note,
                }).ToList();
            await InsertWithExplicitIdsAsync(context, newAffiliations);
            pic.Affiliations = newAffiliations.Count;

            var affiliationEvidence = new List<Tuple<int, PersonEvidencePublicDto>>();
            var validAffiliationIds = new HashSet<int>(existingAffiliationIds.Concat(newAffiliations.Select(a => a.Id)));
            var newAffiliationIds = new HashSet<int>(newAffiliations.Select(a => a.Id));
            foreach (var a in affiliationDtos.Where(a => validAffiliationIds.Contains(a.Id) && (newAffiliationIds.Contains(a.Id) || !affiliationsWithEvidence.Contains(a.Id))))
                foreach (var e in a.Evidence ?? new List<PersonEvidencePublicDto>())
                    affiliationEvidence.Add(Tuple.Create(a.Id, e));
            pic.Evidences += await ImportPersonEvidencesAsync(context, affiliationEvidence,
                (affiliationId, e, text) => new GanjoorPersonAffiliationEvidence
                {
                    AffiliationId = affiliationId,
                    PoemId = e.PoemId,
                    CoupletIndex = e.CoupletIndex,
                    CoupletText = text,
                    MasterCatId = e.MasterCatId,
                    DateAdded = DateTime.Now,
                });
        }

        /// <summary>
        /// adds evidence rows for the poems that exist locally; the couplet text is rebuilt from the verses
        /// </summary>
        /// <returns>number of added rows</returns>
        private async Task<int> ImportPersonEvidencesAsync<TEvidence>(RMuseumDbContext context, List<Tuple<int, PersonEvidencePublicDto>> evidences,
            Func<int, PersonEvidencePublicDto, string, TEvidence> create) where TEvidence : class
        {
            int added = 0;
            foreach (var chunk in evidences.GroupBy(e => e.Item2.PoemId).Select((g, index) => new { g, index }).GroupBy(x => x.index / 300).Select(x => x.Select(y => y.g).ToList()))
            {
                var poemIds = chunk.Select(g => g.Key).ToList();
                var verses = await context.GanjoorVerses.AsNoTracking()
                                .Where(v => poemIds.Contains(v.PoemId) && v.CoupletIndex != null)
                                .OrderBy(v => v.VOrder)
                                .Select(v => new { v.PoemId, CoupletIndex = v.CoupletIndex.Value, v.Text })
                                .ToListAsync();
                var coupletTexts = verses.GroupBy(v => new { v.PoemId, v.CoupletIndex })
                                .ToDictionary(g => Tuple.Create(g.Key.PoemId, g.Key.CoupletIndex), g => string.Join(" ", g.Select(v => v.Text)).Trim());
                foreach (var group in chunk)
                {
                    foreach (var item in group)
                    {
                        if (!coupletTexts.TryGetValue(Tuple.Create(item.Item2.PoemId, item.Item2.CoupletIndex), out string text))
                            continue; // poem (or couplet) is not in this database
                        context.Add(create(item.Item1, item.Item2, text));
                        added++;
                    }
                }
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
            }
            return added;
        }
    }
}
