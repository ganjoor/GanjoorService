using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RMuseum.Models.Artifact.ViewModels;
using RMuseum.Models.Bookmark;
using RMuseum.Models.Bookmark.ViewModels;
using RMuseum.Models.GanjoorIntegration;
using RMuseum.Models.GanjoorIntegration.ViewModels;
using RMuseum.Models.ImportJob;
using RMuseum.Models.Note;
using RMuseum.Models.Note.ViewModels;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IArtifactService implementation
    /// </summary>
    public partial class ArtifactService : IArtifactService
    {

        /// <summary>
        /// get all artifacts (including CoverImage info but not items or tagibutes info)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items)>> GetAll(PagingParameterModel paging, PublishStatus[] statusArray)
        {
            try
            {              
                var source =
                     _context.Artifacts
                     .Include(a => a.CoverImage)
                     .Where(a => statusArray.Contains(a.Status))
                    .OrderByDescending(t => t.DateTime)
                    .AsQueryable();

                (PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items) paginatedResult =
                    await QueryablePaginator<RArtifactMasterRecord>.Paginate(source, paging);



                return new RServiceResult<(PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items)>((PagingMeta: null, Items: null), exp.ToString());
            }
        }

        /// <summary>
        /// get tagged publish artifacts (including CoverImage info but not items or tagibutes info) 
        /// </summary>
        /// <param name="tagUrl"></param>
        /// <param name="valueUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactMasterRecord[]>> GetByTagValue(string tagUrl, string valueUrl, PublishStatus[] statusArray)
        {
            try
            {
                RTag tag =
                            await _context.Tags
                            .Where(a => a.FriendlyUrl == tagUrl)
                        .SingleOrDefaultAsync();
                if(tag == null)
                    return new RServiceResult<RArtifactMasterRecord[]>(new RArtifactMasterRecord[] { });


                RArtifactMasterRecord[] taggedItems =
                    await _context.Artifacts.Include(a => a.Tags)
                     .Include(a => a.CoverImage)
                    .Where(a => statusArray.Contains(a.Status) && a.Tags != null && a.Tags.Any(v => v.RTagId == tag.Id && v.FriendlyUrl == valueUrl))
                    .OrderByDescending(t => t.DateTime)
                    .AsNoTracking()
                    .ToArrayAsync();

                foreach (RArtifactMasterRecord taggedItem in taggedItems)
                    taggedItem.Tags = null;

                return new RServiceResult<RArtifactMasterRecord[]>(taggedItems);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RArtifactMasterRecord[]>(null, exp.ToString());
            }
        }


        /// <summary>
        /// gets specified artifact info (including CoverImage + images +  tagibutes)
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactMasterRecordViewModel>> GetByFriendlyUrl(string friendlyUrl, PublishStatus[] statusArray)
        {
            try
            {
                RArtifactMasterRecord item =
                     await _context.Artifacts
                     .Include(a => a.CoverImage)
                     .Include(a => a.Items).ThenInclude(i => i.Images)
                     .Include(a => a.Items).ThenInclude(i => i.Tags).ThenInclude(t => t.RTag)
                     .Include(a => a.Tags).ThenInclude(t => t.RTag)
                     .Where(a => statusArray.Contains(a.Status) && a.FriendlyUrl == friendlyUrl)
                     .AsNoTracking()
                    .SingleOrDefaultAsync();

                if(item != null)
                {
                   
                   return new RServiceResult<RArtifactMasterRecordViewModel>(new RArtifactMasterRecordViewModel(item));
                }


                return new RServiceResult<RArtifactMasterRecordViewModel>(null);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RArtifactMasterRecordViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// gets specified artifact info (including CoverImage + images +  tagibutes)
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="statusArray"></param>
        /// <param name="tagFriendlyUrl"></param>
        /// <param name="tagValueFriendlyUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactMasterRecordViewModel>> GetByFriendlyUrlFilterItemsByTag(string friendlyUrl, PublishStatus[] statusArray, string tagFriendlyUrl, string tagValueFriendlyUrl)
        {
            try
            {
                RTag rTag = await _context.Tags.Where(t => t.FriendlyUrl == tagFriendlyUrl).SingleOrDefaultAsync();

                if (rTag == null)
                    return new RServiceResult<RArtifactMasterRecordViewModel>(null, "Tag not found!");

                RArtifactMasterRecord artifact =
                     await _context.Artifacts
                     .Include(a => a.CoverImage)
                     .Include(a => a.Items).ThenInclude(i => i.Images)
                     .Include(a => a.Items).ThenInclude(i => i.Tags)
                     .Include(a => a.Tags).ThenInclude(t => t.RTag)
                     .Where(a => statusArray.Contains(a.Status) && a.FriendlyUrl == friendlyUrl)
                     .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (artifact != null)
                {
                    List<RArtifactItemRecord> filteredItems = new List<RArtifactItemRecord>();                 
                    
                    foreach(RArtifactItemRecord item in artifact.Items)
                    {
                        if(item.Tags.Any(v => v.RTagId == rTag.Id  && (string.IsNullOrEmpty(tagValueFriendlyUrl) || v.FriendlyUrl == tagValueFriendlyUrl) ))
                        {
                            item.Tags = null;
                            filteredItems.Add(item);
                        }
                    }

                    artifact.Items = filteredItems;

                    return new RServiceResult<RArtifactMasterRecordViewModel>(new RArtifactMasterRecordViewModel(artifact));
                }


                return new RServiceResult<RArtifactMasterRecordViewModel>(null);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RArtifactMasterRecordViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// edit master record
        /// </summary>
        /// <param name="edited"></param>
        /// <param name="canChangeStatusToAwaiting"></param>
        /// <param name="canPublish"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactMasterRecord>> EditMasterRecord(RArtifactMasterRecord edited, bool canChangeStatusToAwaiting, bool canPublish)
        {
            try
            {

                if(string.IsNullOrEmpty(edited.Name))
                {
                    return new RServiceResult<RArtifactMasterRecord>(null, "Name could not be empty.");
                }

                RArtifactMasterRecord item =
                     await _context.Artifacts
                     .Where(a => a.Id == edited.Id)
                    .SingleOrDefaultAsync();
                   

                if (item != null)
                {
                    if(item.Status != edited.Status)
                    {
                        if(!canChangeStatusToAwaiting)
                        {
                            return new RServiceResult<RArtifactMasterRecord>(null, "User should be able to change status to Awaiting to complete this operation.");
                        }

                        if(
                            !
                            (
                            (item.Status == PublishStatus.Draft && edited.Status == PublishStatus.Awaiting)
                            ||
                            (item.Status == PublishStatus.Awaiting && edited.Status == PublishStatus.Draft)
                            )
                            )
                        {
                            if(!canPublish)
                            {
                                return new RServiceResult<RArtifactMasterRecord>(null, "User should have Publish permission to complete this operation.");
                            }
                        }
                    }

                    item.FriendlyUrl = edited.FriendlyUrl;
                    item.Status = edited.Status;
                    item.Name = edited.Name;
                    item.NameInEnglish = edited.Name;
                    item.Description = edited.Description;
                    item.DescriptionInEnglish = edited.DescriptionInEnglish;
                    item.LastModified = DateTime.Now;

                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }


                return new RServiceResult<RArtifactMasterRecord>(item);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RArtifactMasterRecord>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Set Artifact Cover Item Index
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="itemIndex"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SetArtifactCoverItemIndex(Guid artifactId, int itemIndex)
        {
            try
            {
                RArtifactMasterRecord artifact = await _context
                    .Artifacts.Where(a => a.Id == artifactId)
                    .Include(a => a.Items).ThenInclude(i => i.Images)
                    .SingleOrDefaultAsync();
                if(artifact == null)
                    return new RServiceResult<bool>(false, "Artifact not found.");

                if (itemIndex == artifact.CoverItemIndex)
                    return new RServiceResult<bool>(true);

                if(itemIndex < 0 || itemIndex >= artifact.Items.Count())
                    return new RServiceResult<bool>(false, "Item not found.");

                artifact.CoverItemIndex = itemIndex;
                artifact.CoverImage = RPictureFile.Duplicate(artifact.Items.ToArray()[itemIndex].Images.First());

                artifact.LastModified = DateTime.Now;

                _context.Artifacts.Update(artifact);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get tag bundle by frindly url
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagBundleViewModel>> GetTagBundleByFiendlyUrl(string friendlyUrl)
        {
            try
            {
                RTag tag =
                     await _context.Tags
                     .Where(a => a.FriendlyUrl == friendlyUrl)
                     .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (tag != null)
                {
                    RTagBundleViewModel viewModel = new RTagBundleViewModel()
                    {
                        Id = tag.Id,
                        Name = tag.Name,
                        PluralName = tag.PluralName,
                        FriendlyUrl = friendlyUrl
                    };
                    List<RTagBundleValueViewModel>
                        values = new List<RTagBundleValueViewModel>(
                        await _context.TagValues
                            .Where(value => value.RTagId == tag.Id && !string.IsNullOrEmpty(value.FriendlyUrl))
                            .GroupBy(value => new { value.Value, value.FriendlyUrl, value.RTagId })
                            .Select
                            (
                            g =>
                            new RTagBundleValueViewModel()
                            {
                                FriendlyUrl = g.Key.FriendlyUrl,
                                Name = g.Key.Value,
                                Count = g.Count(),
                                ImageId = _context.Artifacts.Include(a => a.Tags).Where(a => a.Status == PublishStatus.Published && a.Tags.Any(t => t.RTagId == g.Key.RTagId && t.Value == g.Key.Value)).FirstOrDefault().CoverImageId
                            }
                            )
                            .OrderByDescending(g => g.Count)
                            .ToArrayAsync()
                            );
                   

                    viewModel.Values = values.ToArray();
                    
                    return new RServiceResult<RTagBundleViewModel>(viewModel);
                }


                return new RServiceResult<RTagBundleViewModel>(null);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTagBundleViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get max lastmodified artifact date for caching purposes
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<DateTime>> GetMaxArtifactLastModified()
        {
            try
            {
                return new RServiceResult<DateTime>(await _context.Artifacts.MaxAsync(a => a.LastModified));
            }
            catch(Exception exp)
            {
                return new RServiceResult<DateTime>(DateTime.Now, exp.ToString());
            }
        }

        /// <summary>
        /// get all  tags 
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RTag[] Items)>> GetAllTags(PagingParameterModel paging)
        {
            try
            {
                var source =
                     _context.Tags
                    .OrderByDescending(t => t.Order)
                    .AsQueryable();

                (PaginationMetadata PagingMeta, RTag[] Items) paginatedResult =
                    await QueryablePaginator<RTag>.Paginate(source, paging);



                return new RServiceResult<(PaginationMetadata PagingMeta, RTag[] Items)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, RTag[] Items)>((PagingMeta: null, Items: null), exp.ToString());
            }
        }

        /// <summary>
        /// get tag bu friendly url
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTag>> GetTagByFriendlyUrl(string friendlyUrl)
        {
            try
            {                
                return new RServiceResult<RTag>(await _context.Tags.Where(t => t.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTag>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get tag value by frindly url
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagValue>> GetTagValueByFriendlyUrl(Guid tagId, string friendlyUrl)
        {
            try
            {
                RTagValue value = await _context.TagValues.Where(t => t.RTagId == tagId && t.FriendlyUrl == friendlyUrl).FirstOrDefaultAsync();
                if (value != null)
                    return new RServiceResult<RTagValue>(value);
                return new RServiceResult<RTagValue>(null, "Tag value not found");
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTagValue>(null, exp.ToString());
            }
        }

        /// <summary>
        /// add tag
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<RTag>> AddTag(string tagName)
        {
            try
            {

                if (string.IsNullOrEmpty(tagName))
                {
                    return new RServiceResult<RTag>(null, "Name could not be empty.");
                }

                RTag existingTag =
                     await _context.Tags
                     .Where(a => a.Name == tagName || a.NameInEnglish == tagName)
                    .SingleOrDefaultAsync();
                if(existingTag != null)
                {
                    return new RServiceResult<RTag>(null, "Duplicated Name or English Name for tag.");
                }

                int maxOrder = await _context.Tags.CountAsync() == 0 ? 0 : await _context.Tags.MaxAsync(a => a.Order);
                RTag type = new RTag()
                {
                    Name = tagName,
                    NameInEnglish = tagName,
                    PluralName = $"{tagName}s",
                    PluralNameInEnglish = $"{tagName}s",
                    Order = maxOrder + 1,
                    Status = PublishStatus.Published,
                    GlobalValue = true
                };
                await _context.Tags.AddAsync(type);
                await _context.SaveChangesAsync();


                return new RServiceResult<RTag>(type);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTag>(null, exp.ToString());
            }
        }


        /// <summary>
        /// edit tag
        /// </summary>
        /// <param name="edited"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTag>> EditTag(RTag edited)
        {
            try
            {
                if (string.IsNullOrEmpty(edited.Name))
                {
                    return new RServiceResult<RTag>(null, "Name could not be empty.");
                }

                RTag tag =
                     await _context.Tags
                     .Where(a => a.Id == edited.Id)
                    .SingleOrDefaultAsync();


                if (tag != null)
                {
                    tag.Name = edited.Name;
                    tag.Order = edited.Order;
                    tag.FriendlyUrl = edited.FriendlyUrl;
                    tag.TagType = edited.TagType;
                    tag.Status = edited.Status;
                    tag.NameInEnglish = edited.NameInEnglish;
                    tag.GlobalValue = edited.GlobalValue;
                    tag.PluralName = edited.PluralName;
                    tag.PluralNameInEnglish = edited.PluralNameInEnglish;
                    _context.Update(tag);

                    

                    RArtifactMasterRecord[] taggedItems =
                        await _context.Artifacts.Include(a => a.Tags)
                        .Where(a => a.Tags != null && a.Tags.Any(v => v.RTagId == tag.Id))
                        .ToArrayAsync();                      

                    foreach(RArtifactMasterRecord taggedItem in taggedItems)
                    {
                        taggedItem.LastModified = DateTime.Now;
                        _context.Update(taggedItem);
                    }

                    RArtifactItemRecord[] taggedPages =
                        await _context.Items.Include(a => a.Tags)
                        .Where(a => a.Tags != null && a.Tags.Any(v => v.RTagId == tag.Id))
                        .ToArrayAsync();

                    foreach (RArtifactItemRecord taggedItem in taggedPages)
                    {
                        taggedItem.LastModified = DateTime.Now;
                        _context.Update(taggedItem);
                    }

                    await UpdateTaggedItems(tag);

                    await _context.SaveChangesAsync();
                }


                return new RServiceResult<RTag>(tag);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTag>(null, exp.ToString());
            }
        }

        /// <summary>
        /// changes LastModified field value for tagged artifacts and items
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private async Task UpdateTaggedItems(RTag tag)
        {
            RArtifactMasterRecord[] taggedItems =
                        await _context.Artifacts.Include(a => a.Tags)
                        .Where(a => a.Tags != null && a.Tags.Any(v => v.RTagId == tag.Id))
                        .ToArrayAsync();

            foreach (RArtifactMasterRecord taggedItem in taggedItems)
            {
                taggedItem.LastModified = DateTime.Now;
                _context.Update(taggedItem);
            }

            RArtifactItemRecord[] taggedPages =
                await _context.Items.Include(a => a.Tags)
                .Where(a => a.Tags != null && a.Tags.Any(v => v.RTagId == tag.Id))
                .ToArrayAsync();

            foreach (RArtifactItemRecord taggedItem in taggedPages)
            {
                taggedItem.LastModified = DateTime.Now;
                _context.Update(taggedItem);
            }
        }

        /// <summary>
        /// changes order of tags based on their position in artifacts
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="artifactId"></param>
        /// <param name="up">true => up, false => down</param>
        /// <returns>the other tag which its Order has been changed</returns>
        public async Task<RServiceResult<Guid?>> EditTagOrderBasedOnArtifact(Guid tagId, Guid artifactId, bool up)
        {
            try
            {
                RArtifactMasterRecord item =
                     await _context.Artifacts
                     .Include(a => a.Tags).ThenInclude(t => t.RTag)
                     .Where(a => a.Id == artifactId)
                     .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (item == null)
                {
                    return new RServiceResult<Guid?>(null, "artifact not found");
                }

                RArtifactMasterRecordViewModel viewModel = new RArtifactMasterRecordViewModel(item); //tags are sorted in this method

                int tagOrder = viewModel.ArtifactTags.Where(tag => tag.Id == tagId).FirstOrDefault().Order;
                RArtifactTagViewModel otherTagViewModel;
                if(up)
                {
                    otherTagViewModel = viewModel.ArtifactTags.Where(tag => tag.Order < tagOrder).OrderByDescending(tag => tag.Order).FirstOrDefault();
                }
                else
                {
                    otherTagViewModel = viewModel.ArtifactTags.Where(tag => tag.Order > tagOrder).OrderBy(tag => tag.Order).FirstOrDefault();
                }

                if(otherTagViewModel == null)
                {
                    return new RServiceResult<Guid?>(null, "Invalid movement");
                }

                RTag rTag1 = await _context.Tags.Where(tag => tag.Id == tagId).FirstOrDefaultAsync();
                RTag rTag2 = await _context.Tags.Where(tag => tag.Id == otherTagViewModel.Id ).FirstOrDefaultAsync();

                rTag1.Order = otherTagViewModel.Order;
                rTag2.Order = tagOrder;


                _context.Tags.UpdateRange(new RTag[] { rTag1, rTag2 });


                await UpdateTaggedItems(rTag1);
                await UpdateTaggedItems(rTag2);

                await _context.SaveChangesAsync();

                return new RServiceResult<Guid?>((Guid?)rTag2.Id);


            }
            catch(Exception exp)
            {
                return new RServiceResult<Guid?>(null, exp.ToString());
            }
        }


        /// <summary>
        /// changes order of tags based on their position in artifact items
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="itemId"></param>
        /// <param name="up">true => up, false => down</param>
        /// <returns>the other tag which its Order has been changed</returns>
        public async Task<RServiceResult<Guid?>> EditTagOrderBasedOnItem(Guid tagId, Guid itemId, bool up)
        {
            try
            {
                RArtifactItemRecord item =
                     await _context.Items
                     .Include(a => a.Tags).ThenInclude(t => t.RTag)
                     .Where(a => a.Id == itemId)
                     .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (item == null)
                {
                    return new RServiceResult<Guid?>(null, "item not found");
                }

                RArtifactItemRecordViewModel viewModel = new RArtifactItemRecordViewModel(); //tags are sorted in this method
                viewModel.Item = item;

                int tagOrder = viewModel.FormattedTags.Where(tag => tag.Id == tagId).FirstOrDefault().Order;
                RArtifactTagViewModel otherTagViewModel;
                if (up)
                {
                    otherTagViewModel = viewModel.FormattedTags.Where(tag => tag.Order < tagOrder).OrderByDescending(tag => tag.Order).FirstOrDefault();
                }
                else
                {
                    otherTagViewModel = viewModel.FormattedTags.Where(tag => tag.Order > tagOrder).OrderBy(tag => tag.Order).FirstOrDefault();
                }

                if (otherTagViewModel == null)
                {
                    return new RServiceResult<Guid?>(null, "Invalid movement");
                }

                RTag rTag1 = await _context.Tags.Where(tag => tag.Id == tagId).FirstOrDefaultAsync();
                RTag rTag2 = await _context.Tags.Where(tag => tag.Id == otherTagViewModel.Id).FirstOrDefaultAsync();

                rTag1.Order = otherTagViewModel.Order;
                rTag2.Order = tagOrder;


                _context.Tags.UpdateRange(new RTag[] { rTag1, rTag2 });


                await UpdateTaggedItems(rTag1);
                await UpdateTaggedItems(rTag2);

                await _context.SaveChangesAsync();

                return new RServiceResult<Guid?>((Guid?)rTag2.Id);


            }
            catch (Exception exp)
            {
                return new RServiceResult<Guid?>(null, exp.ToString());
            }
        }


        /// <summary>
        /// get tag value bundle by frindly url
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="valueUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactTagViewModel>> GetTagValueBundleByFiendlyUrl(string friendlyUrl, string valueUrl)
        {
            try
            {
                RTag tag =
                     await _context.Tags
                     .Where(a => a.FriendlyUrl == friendlyUrl)
                     .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (tag != null)
                {
                    RArtifactTagViewModel viewModel
                        = new RArtifactTagViewModel()
                        {
                            Id = tag.Id,
                            Order = tag.Order,
                            TagType = tag.TagType,
                            FriendlyUrl = tag.FriendlyUrl,
                            Status = tag.Status,
                            Name = tag.Name,
                            NameInEnglish = tag.NameInEnglish,
                            GlobalValue = tag.GlobalValue,
                            PluralName = tag.PluralName,
                            PluralNameInEnglish = tag.PluralNameInEnglish
                        };
                       
                    viewModel.Values =
                        new RTagValue[]
                        {
                        await _context.TagValues
                            .Where(value => value.RTagId == tag.Id && value.FriendlyUrl == valueUrl)
                            .FirstOrDefaultAsync()
                        };
                            

                    return new RServiceResult<RArtifactTagViewModel>(viewModel);
                }


                return new RServiceResult<RArtifactTagViewModel>(null);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RArtifactTagViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// add artifact tag value
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="rTag"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagValue>> TagArtifact(Guid artifactId, RTag rTag)
        {
            try
            {
                RTag type = await _context.Tags.Where(a => a.Id == rTag.Id).SingleOrDefaultAsync();

                RArtifactMasterRecord item = await _context.Artifacts.Include(i => i.Tags).Where(i => i.Id == artifactId).SingleOrDefaultAsync();

                int order = item.Tags.Where(t => t.RTagId == type.Id).Count() == 0 ? 1 : item.Tags.Where(t => t.RTagId == type.Id).OrderByDescending(t => t.Order).FirstOrDefault().Order + 1;

                RTagValue tag =
                new RTagValue()
                {
                    Order = order,
                    Value = "",
                    ValueInEnglish = "",
                    ValueSupplement = "",
                    RTag = type,
                    Status = PublishStatus.Published
                };

                item.Tags.Add(tag);
                item.LastModified = DateTime.Now;
                _context.Artifacts.Update(item);
                await _context.SaveChangesAsync();

                return new RServiceResult<RTagValue>(tag);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTagValue>(null, exp.ToString());
            }
        }

        /// <summary>
        /// remove artfiact tag value
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UnTagArtifact(Guid artifactId, Guid tagValueId)
        {
            try
            {

                RArtifactMasterRecord item = await _context.Artifacts.Include(i => i.Tags).Where(i => i.Id == artifactId).SingleOrDefaultAsync();
                item.Tags.Remove(item.Tags.Where(t => t.Id == tagValueId).SingleOrDefault());
                item.LastModified = DateTime.Now;
                _context.Artifacts.Update(item);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// edit artifact tag value
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="edited"></param>
        /// <param name="global">apply on all same value tags</param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagValue>> EditTagValue(Guid artifactId, RTagValue edited, bool global)
        {
            try
            {
                if (string.IsNullOrEmpty(edited.Value))
                {
                    return new RServiceResult<RTagValue>(null, "Value could not be empty.");
                }

                RArtifactMasterRecord artifact =
                    await _context.Artifacts
                     .Include(a => a.Tags)
                     .Where(a => a.Id == artifactId)
                    .SingleOrDefaultAsync();
                if(artifact == null)
                    return new RServiceResult<RTagValue>(null);

                RTagValue tag =
                    artifact.Tags.Where(a => a.Id == edited.Id)
                    .SingleOrDefault();                   


                if (tag != null)
                {
                    tag.Order = edited.Order;
                    tag.ValueSupplement = edited.ValueSupplement;
                    _context.Update(tag);

                    if (global)
                    {
                        RTagValue[] sameValueTags = await _context.TagValues.Where(v => v.Value == tag.Value && v.RTagId == tag.RTagId).ToArrayAsync();
                        foreach(RTagValue sameValueTag in sameValueTags)
                        {
                            sameValueTag.Value = edited.Value;
                            sameValueTag.ValueInEnglish = edited.ValueInEnglish;
                            sameValueTag.Status = edited.Status;
                            sameValueTag.FriendlyUrl = edited.FriendlyUrl;
                            _context.Update(sameValueTag);

                            RArtifactMasterRecord correspondingArtifact = 
                                await _context.Artifacts.Include(a => a.Tags).Where(a => a.Tags.Contains(sameValueTag)).SingleOrDefaultAsync();
                            if(correspondingArtifact != null)
                            {
                                correspondingArtifact.LastModified = DateTime.Now;
                                _context.Update(correspondingArtifact);
                            }

                            RArtifactItemRecord correspondingItem =
                                await _context.Items.Include(a => a.Tags).Where(a => a.Tags.Contains(sameValueTag)).SingleOrDefaultAsync();
                            if(correspondingItem != null)
                            {
                                correspondingItem.LastModified = DateTime.Now;
                                _context.Update(correspondingItem);
                            }                           
                            
                        }
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        tag.Value = edited.Value;
                        tag.ValueInEnglish = edited.ValueInEnglish;
                        tag.Order = edited.Order;
                        tag.FriendlyUrl = edited.FriendlyUrl;
                        tag.Status = edited.Status;
                        tag.ValueSupplement = edited.ValueSupplement;
                        _context.Update(tag);
                        artifact.LastModified = DateTime.Now;
                        _context.Update(artifact);
                        await _context.SaveChangesAsync();
                    }
                }


                return new RServiceResult<RTagValue>(tag);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTagValue>(null, exp.ToString());
            }
        }

        
               /// <summary>
        /// add item tag value
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="rTag"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagValue>> TagItem(Guid itemId, RTag rTag)
        {
            try
            {
                RTag type = await _context.Tags.Where(a => a.Id == rTag.Id).SingleOrDefaultAsync();

                RArtifactItemRecord item = await _context.Items.Include(i => i.Tags).Where(i=> i.Id == itemId).SingleOrDefaultAsync();

                int order = item.Tags.Where(t => t.RTagId == type.Id).Count() == 0 ? 1 : item.Tags.Where(t => t.RTagId == type.Id).OrderByDescending(t => t.Order).FirstOrDefault().Order + 1;

                RTagValue tag =
                new RTagValue()
                {
                    Order = order,
                    Value = "",
                    ValueInEnglish = "",
                    ValueSupplement = "",
                    RTag = type,
                    Status = PublishStatus.Published
                };

                item.Tags.Add(tag);
                item.LastModified = DateTime.Now;
                _context.Items.Update(item);
                await _context.SaveChangesAsync();

                return new RServiceResult<RTagValue>(tag);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTagValue>(null, exp.ToString());
            }
        }

        /// <summary>
        /// remove item tag value
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UnTagItem(Guid itemId, Guid tagValueId)
        {
            try
            {
              
                RArtifactItemRecord item = await _context.Items.Include(i => i.Tags).Where(i => i.Id == itemId).SingleOrDefaultAsync();
                item.Tags.Remove(item.Tags.Where(t => t.Id == tagValueId).SingleOrDefault());
                item.LastModified = DateTime.Now;
                _context.Items.Update(item);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// edit item tagibute value
        /// </summary>
        /// <param name="itemtId"></param>
        /// <param name="edited"></param>
        /// <param name="global">apply on all same value tags</param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagValue>> EditItemTagValue(Guid itemtId, RTagValue edited, bool global)
        {
            try
            {
                if (string.IsNullOrEmpty(edited.Value))
                {
                    return new RServiceResult<RTagValue>(null, "Value could not be empty.");
                }

                RArtifactItemRecord item =
                    await _context.Items
                     .Include(a => a.Tags)
                     .Where(a => a.Id == itemtId)
                    .SingleOrDefaultAsync();
                if (item == null)
                    return new RServiceResult<RTagValue>(null);

                RTagValue tag =
                    item.Tags.Where(a => a.Id == edited.Id)
                    .SingleOrDefault();


                if (tag != null)
                {
                    tag.Order = edited.Order;
                    tag.ValueSupplement = edited.ValueSupplement;
                    _context.Update(tag);

                    if (global)
                    {
                        RTagValue[] sameValueTags = await _context.TagValues.Where(v => v.Value == tag.Value && v.RTagId == tag.RTagId).ToArrayAsync();
                        foreach (RTagValue sameValueTag in sameValueTags)
                        {
                            sameValueTag.Value = edited.Value;
                            sameValueTag.ValueInEnglish = edited.ValueInEnglish;
                            sameValueTag.Status = edited.Status;
                            sameValueTag.FriendlyUrl = edited.FriendlyUrl;
                            
                            _context.Update(sameValueTag);

                            RArtifactMasterRecord correspondingArtifact =
                                await _context.Artifacts.Include(a => a.Tags).Where(a => a.Tags.Contains(sameValueTag)).SingleOrDefaultAsync();
                            if (correspondingArtifact != null)
                            {
                                correspondingArtifact.LastModified = DateTime.Now;
                                _context.Update(correspondingArtifact);
                            }

                            RArtifactItemRecord correspondingItem =
                                await _context.Items.Include(a => a.Tags).Where(a => a.Tags.Contains(sameValueTag)).SingleOrDefaultAsync();
                            if (correspondingItem != null)
                            {
                                correspondingItem.LastModified = DateTime.Now;
                                _context.Update(correspondingItem);
                            }

                        }
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        tag.Value = edited.Value;
                        tag.ValueInEnglish = edited.ValueInEnglish;
                        tag.Order = edited.Order;
                        tag.FriendlyUrl = edited.FriendlyUrl;
                        tag.Status = edited.Status;
                        tag.ValueSupplement = edited.ValueSupplement;
                        _context.Update(tag);
                        item.LastModified = DateTime.Now;
                        _context.Update(item);
                        await _context.SaveChangesAsync();
                    }
                }


                return new RServiceResult<RTagValue>(tag);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RTagValue>(null, exp.ToString());
            }
        }

        /// <summary>
        /// changes order of tag values based on their position in an artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="tagId"></param>
        /// <param name="valueId"></param>        
        /// <param name="up">true => up, false => down</param>
        /// <returns>the other tag value which its Order has been changed</returns>
        public async Task<RServiceResult<Guid?>> EditTagValueOrder(Guid artifactId, Guid tagId, Guid valueId, bool up)
        {
            try
            {
                RArtifactMasterRecord item =
                     await _context.Artifacts
                     .Include(a => a.Tags).ThenInclude(t => t.RTag)
                     .Where(a => a.Id == artifactId)
                    .SingleOrDefaultAsync();

                if (item == null)
                {
                    return new RServiceResult<Guid?>(null, "artifact not found");
                }

                RArtifactMasterRecordViewModel viewModel = new RArtifactMasterRecordViewModel(item); //tags are sorted in this method

                RArtifactTagViewModel tag = viewModel.ArtifactTags.Where(t => t.Id == tagId).FirstOrDefault();
                RTagValue value1 = tag.Values.Where(v => v.Id == valueId).FirstOrDefault();
                RTagValue value2;
                if (up)
                {
                    value2 = tag.Values.Where(v => v.Order < value1.Order).OrderByDescending(v => v.Order).FirstOrDefault();
                }
                else
                {
                    value2 = tag.Values.Where(v => v.Order > value1.Order).OrderBy(v => v.Order).FirstOrDefault();
                }

                if (value2 == null)
                {
                    return new RServiceResult<Guid?>(null, "Invalid movement");
                }


                int nOrder = value1.Order;
                value1.Order = value2.Order;
                value2.Order = nOrder;

                _context.TagValues.UpdateRange(new RTagValue[] { value1, value2 });

                item.LastModified = DateTime.Now;
                _context.Artifacts.Update(item);

                await _context.SaveChangesAsync();

                return new RServiceResult<Guid?>((Guid?)value2.Id);


            }
            catch (Exception exp)
            {
                return new RServiceResult<Guid?>(null, exp.ToString());
            }
        }

        /// <summary>
        /// changes order of tag values based on their position in an item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tagId"></param>
        /// <param name="valueId"></param>        
        /// <param name="up">true => up, false => down</param>
        /// <returns>the other tag value which its Order has been changed</returns>
        public async Task<RServiceResult<Guid?>> EditItemTagValueOrder(Guid itemId, Guid tagId, Guid valueId, bool up)
        {
            try
            {
                RArtifactItemRecord item =
                     await _context.Items
                     .Include(a => a.Tags).ThenInclude(t => t.RTag)
                     .Where(a => a.Id == itemId)
                    .SingleOrDefaultAsync();

                if (item == null)
                {
                    return new RServiceResult<Guid?>(null, "item not found");
                }

                RArtifactItemRecordViewModel viewModel = new RArtifactItemRecordViewModel(); //tags are sorted in this method
                viewModel.Item = item;

                RArtifactTagViewModel tag = viewModel.FormattedTags.Where(t => t.Id == tagId).FirstOrDefault();
                RTagValue value1 = tag.Values.Where(v => v.Id == valueId).FirstOrDefault();
                RTagValue value2;
                if (up)
                {
                    value2 = tag.Values.Where(v => v.Order < value1.Order).OrderByDescending(v => v.Order).FirstOrDefault();
                }
                else
                {
                    value2 = tag.Values.Where(v => v.Order > value1.Order).OrderBy(v => v.Order).FirstOrDefault();
                }

                if (value2 == null)
                {
                    return new RServiceResult<Guid?>(null, "Invalid movement");
                }


                int nOrder = value1.Order;
                value1.Order = value2.Order;
                value2.Order = nOrder;

                _context.TagValues.UpdateRange(new RTagValue[] { value1, value2 });

                item.LastModified = DateTime.Now;
                _context.Items.Update(item);

                await _context.SaveChangesAsync();

                return new RServiceResult<Guid?>((Guid?)value2.Id);


            }
            catch (Exception exp)
            {
                return new RServiceResult<Guid?>(null, exp.ToString());
            }
        }

        /// <summary>
        /// gets specified artifact item info (including images + tagibutes)
        /// </summary>
        /// <param name="artifactUrl"></param>
        /// <param name="itemUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactItemRecordViewModel>> GetArtifactItemByFrienlyUrl(string artifactUrl, string itemUrl, PublishStatus[] statusArray)
        {
            try
            {
                RArtifactMasterRecord parent =
                     await _context.Artifacts
                     .Include(a => a.CoverImage)
                     .Include(a => a.Items).ThenInclude(i => i.Images)
                     .Include(a => a.Items).ThenInclude(i => i.Tags).ThenInclude(t => t.RTag)
                     .Where(a => statusArray.Contains(a.Status) && a.FriendlyUrl == artifactUrl)
                     .AsNoTracking()
                     .SingleOrDefaultAsync();

                if (parent == null)
                    return new RServiceResult<RArtifactItemRecordViewModel>(null);

                if(parent.Items != null)
                    parent.Items = parent.Items.OrderBy(i => i.Order).ToArray();
                if(parent.Tags != null)
                    parent.Tags = parent.Tags.OrderBy(a => a.RTag.Order).ToArray();



                RArtifactItemRecord item =
                     parent.Items
                     .Where(i => i.FriendlyUrl == itemUrl)
                     .SingleOrDefault();

                if (item == null)
                    return new RServiceResult<RArtifactItemRecordViewModel>(null);

                if (item.Images != null)
                    item.Images = item.Images.OrderBy(i => i.Order).ToArray();
                if (item.Tags != null)
                    item.Tags = item.Tags.OrderBy(a => a.RTag.Order).ToArray();

                RArtifactItemRecord nextItem = parent.Items.Where(i => i.Order > item.Order).OrderBy(i => i.Order).FirstOrDefault();
                RArtifactItemRecord prevItem = parent.Items.Where(i => i.Order < item.Order).OrderByDescending(i => i.Order).FirstOrDefault();

                RArtifactItemRecordViewModel res = new RArtifactItemRecordViewModel()
                {
                    Item = item,
                    ParentFriendlyUrl = parent.FriendlyUrl,
                    ParentName = parent.Name,
                    ParentImageId = parent.CoverImage.Id,
                    ParentItemCount = parent.Items.Count(),
                    NextItemFriendlyUrl = nextItem == null ? "" : nextItem.FriendlyUrl,
                    NextItemImageId = nextItem == null ? (Guid?)null : nextItem.Images.First().Id,
                    PreviousItemFriendlyUrl = prevItem == null ? "" : prevItem.FriendlyUrl,
                    PrevItemImageId = prevItem == null ? (Guid?)null : prevItem.Images.First().Id,
                };

                return new RServiceResult<RArtifactItemRecordViewModel>(res);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RArtifactItemRecordViewModel>(null, exp.ToString());
            }
        }



        /// <summary>
        /// add new artifact
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="srcurl"></param>
        /// <param name="pictitle"></param>
        /// <param name="picdescription"></param>
        /// <param name="file"></param>
        /// <param name="picsrcurl"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactMasterRecord>> Add
            (
            string name, string description, string srcurl,
            string pictitle, string picdescription, IFormFile file, string picsrcurl, Stream stream
            )
        {
            try
            {
                RServiceResult<RPictureFile> picture = await _pictureFileService.Add(pictitle, picdescription, 1, file, picsrcurl, stream, "", "");
                if (picture.Result == null)
                {
                    return new RServiceResult<RArtifactMasterRecord>(null, $"_pictureFileService.Add : {picture.ExceptionString}");
                }

                RArtifactItemRecord item = new RArtifactItemRecord()
                {
                    Order = 1,
                    CoverImageIndex = 0,
                    Images = new RPictureFile[] { picture.Result }
                };

                RArtifactMasterRecord artifact = new RArtifactMasterRecord(name, description)
                {
                    Name = name,
                    NameInEnglish = name,
                    Description = description,
                    DescriptionInEnglish = description,
                    Status = PublishStatus.Draft,
                    DateTime = DateTime.Now,
                    LastModified = DateTime.Now,
                    CoverItemIndex = 0,
                    Items = new RArtifactItemRecord[] { item },
                    ItemCount = 1,
                    CoverImage = picture.Result
                };


                await _context.Artifacts.AddAsync(artifact);
                await _context.SaveChangesAsync();

                return new RServiceResult<RArtifactMasterRecord>(artifact);
            }
            catch(Exception exp)
            {
                return new RServiceResult<RArtifactMasterRecord>(null, exp.ToString());
            }         
                      
        }

        /// <summary>
        /// import from external resources
        /// </summary>
        /// <param name="srcType">loc/princeton/harvard/qajarwomen/hathitrust/penn/cam/bl/folder/walters/cbl</param>
        /// <param name="resourceNumber">119</param>
        /// <param name="friendlyUrl">golestan-baysonghori</param>
        /// <param name="resourcePrefix"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> Import(string srcType, string resourceNumber, string friendlyUrl, string resourcePrefix)
        {
           return
                srcType == "princeton" ?
                await StartImportingFromPrinceton(resourceNumber, friendlyUrl)
                :
                srcType == "harvard" ?
                await StartImportingFromHarvard(resourceNumber, friendlyUrl)
                :
                 srcType == "qajarwomen" ?
                await StartImportingFromHarvardDirectly(resourceNumber, friendlyUrl, resourcePrefix)
                :
                 srcType == "hathitrust" ?
                await StartImportingFromHathiTrust(resourceNumber, friendlyUrl)
                :
                srcType == "penn" ?
                await StartImportingFromPenLibraries(resourceNumber, friendlyUrl)
                :
                srcType == "cam" ?
                await StartImportingFromCambridge(resourceNumber, friendlyUrl)
                :
                srcType == "bl" ?
                await StartImportingFromBritishLibrary(resourceNumber, friendlyUrl)
                :
                srcType == "folder" ?
                await StartImportingFromServerFolder(resourceNumber, friendlyUrl, resourcePrefix)
                :
                srcType == "walters" ?
                await StartImportingFromWalters(resourceNumber, friendlyUrl)
                 :
                srcType == "cbl" ?
                await StartImportingFromChesterBeatty(resourceNumber, friendlyUrl)
                :
                await StartImportingFromTheLibraryOfCongress(resourceNumber, friendlyUrl, resourcePrefix);
        }
       

        /// <summary>
        /// reschedule jobs
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RescheduleJobs()
        {
            try
            {


                ImportJob[] jobs =  await _context.ImportJobs.Where(j => j.Status != ImportJobStatus.Succeeded && j.JobType == JobType.ChesterBeatty).OrderByDescending(j => j.ProgressPercent).ToArrayAsync();

                List<string> scheduled = new List<string>();
                
                foreach(ImportJob job in jobs)
                {              
                    if(job.Status != ImportJobStatus.Failed)
                    {
                        job.Status = ImportJobStatus.Aborted;
                        job.EndTime = DateTime.Now;
                        job.Exception = "Aborted by RescheduleJobs";
                        _context.Update(job);
                        await _context.SaveChangesAsync();
                    }

                    if (
                        await _context.ImportJobs.Where(j => j.ResourceNumber == job.ResourceNumber && j.Status == ImportJobStatus.Succeeded).SingleOrDefaultAsync()
                        !=
                        null
                        )
                        continue;

                    if (scheduled.IndexOf(job.ResourceNumber) == -1)
                    {
                        scheduled.Add(job.ResourceNumber);
                       
                        RServiceResult<bool> rescheduled = await StartImportingFromChesterBeatty(job.ResourceNumber, job.FriendlyUrl);
                        if(rescheduled.Result)
                        {
                            _context.ImportJobs.Remove(job);
                            await _context.SaveChangesAsync();
                        }

                    }
                }
                
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(true, exp.ToString());
            }
        }

        /// <summary>
        /// an incomplete prototype for removing artifacts
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="checkJobs"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RemoveArtifactHavingNoNoteAndBookmarks(Guid artifactId, bool checkJobs)
        {
            try
            {
                RArtifactMasterRecord record = await _context.Artifacts
                        .Include(a => a.Items).ThenInclude(i => i.Images)
                        .Include(a => a.Items).ThenInclude(i => i.Tags)
                        .Include(a => a.Tags)
                        .Where(a => a.Id == artifactId)
                        .SingleOrDefaultAsync();
                if (record == null)
                {
                    return new RServiceResult<bool>(false, "Artifact not found.");
                }
                if (record.Status == PublishStatus.Published)
                {
                    return new RServiceResult<bool>(false, "Can not delete published artifact");
                }

                if(checkJobs)
                {
                    var jobs = await _context.ImportJobs.Where(j => j.ArtifactId == artifactId).ToArrayAsync();
                    if (jobs.Length > 0)
                    {
                        _context.ImportJobs.RemoveRange(jobs);
                    }
                }

                var pins = await _context.PinterestLinks.Where(j => j.ArtifactId == artifactId).ToArrayAsync();
                if (pins.Length > 0)
                {
                    _context.PinterestLinks.RemoveRange(pins);
                }


                foreach (RArtifactItemRecord item in record.Items)
                {
                    _context.PictureFiles.RemoveRange(item.Images);
                    _context.TagValues.RemoveRange(item.Tags);
                }

                _context.Items.RemoveRange(record.Items);
                _context.TagValues.RemoveRange(record.Tags);
                _context.Artifacts.Remove(record);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }            
        }

        /// <summary>
        /// due to a bug in loc json outputs some artifacts with more than 1000 pages were downloaded incompletely
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<string[]>> ReExamineLocDownloads()
        {
            try
            {
                ImportJob[] jobs = await _context.ImportJobs
                    .Include(j => j.Artifact)
                    .Where(j => j.Status == ImportJobStatus.Succeeded && j.JobType == JobType.Loc).ToArrayAsync();

                List<string> scheduled = new List<string>();
                List<ImportJob> rescheduledJobs = new List<ImportJob>();
                foreach (ImportJob job in jobs)
                {

                    int pageCount = 0;
                    //اول یک صفحه را می‌خوانیم تا تعداد صفحات را مشخص کنیم
                    using (var client = new HttpClient())
                    {
                        if (job.Artifact == null)
                            continue;

                        string url = $"https://www.loc.gov/resource/rbc0001.{job.ResourceNumber}/?fo=json&st=gallery"; //plmp

                        var result = await client.GetAsync(url);
                        //using (var result = await client.GetAsync(url))
                        {
                            int _ImportRetryCount = 5;
                            int _ImportRetryInitialSleep = 500;
                            int retryCount = 0;
                            while (retryCount < _ImportRetryCount && !result.IsSuccessStatusCode && result.StatusCode == HttpStatusCode.ServiceUnavailable)
                            {
                                result.Dispose();
                                Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                result = await client.GetAsync(url);
                                retryCount++;
                            }

                            if (result.IsSuccessStatusCode)
                            {
                                string json = await result.Content.ReadAsStringAsync();
                                var parsed = JObject.Parse(json);

                                pageCount = parsed.SelectToken("resource.segment_count").Value<int>();
                            }
                            else
                            {
                                return new RServiceResult<string[]>(null, $"{job.ResourceNumber}: Http result is not ok ({result.StatusCode}) for {url}");
                            }

                            if(pageCount != job.Artifact.ItemCount)
                            {
                                if (scheduled.IndexOf(job.ResourceNumber) == -1)
                                {                                    
                                    scheduled.Add(job.ResourceNumber);
                                    rescheduledJobs.Add(job);

                                }
                            }
                            result.Dispose();
                        }

                    }                   
                }

                scheduled = new List<string>();
                foreach (ImportJob job in rescheduledJobs)
                {
                    await RemoveArtifactHavingNoNoteAndBookmarks((Guid)job.ArtifactId, false);
                    _context.ImportJobs.Remove(job);
                    await _context.SaveChangesAsync();
                    RServiceResult<bool> rescheduled = await StartImportingFromTheLibraryOfCongress(job.ResourceNumber, job.FriendlyUrl, "rbc0001");//plmp
                    if (rescheduled.Result)
                    {
                       
                        scheduled.Add(job.ResourceNumber);
                    }
                }
                return new RServiceResult<string[]>(scheduled.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<string[]>(null, exp.ToString());
            }
        }
        private async Task<string> HandleSimpleValue(RMuseumDbContext context, JObject parsed, List<RTagValue> meta, string path, string aName)
        {
            try
            {
                string string_value = parsed.SelectToken(path).Value<string>();
                if (!string.IsNullOrWhiteSpace(string_value))
                {
                    meta.Add
                    (
                        await TagHandler.PrepareAttribute(context, aName, string_value, 1)
                    );
                }
                return string_value;
            }
            catch
            {
                return "";
            }
        }

        private async Task<string> HandleListValue(RMuseumDbContext context, JObject parsed, List<RTagValue> meta, string path, string aName)
        {
            try
            {
                string value = "";
                List<string> list = parsed.SelectToken(path).ToObject<List<string>>();
                if (list != null && list.Count > 0)
                {
                    value = list[0];
                    meta.Add
                    (
                        await TagHandler.PrepareAttribute(context, aName, list[0], 1)
                    );

                    for (int i = 1; i < list.Count; i++)
                    {
                        value += $"<br/>{Environment.NewLine}{list[i]}";
                        meta.Add
                        (
                            await TagHandler.PrepareAttribute(context, aName, list[i], i + 1)
                        );

                    }
                }
                return value;
            }
            catch
            {
                return "";
            }
        }

        private async Task<string> HandleChildrenValue(RMuseumDbContext context, JObject parsed, List<RTagValue> meta, string path, string aName)
        {
            try
            {
                string value = "";
                var children = parsed.SelectToken(path).Children<JToken>().ToList();
                if (children.Count > 0)
                {
                    value = children[0].First.Path.Replace($"{path}[0].", "").Replace($"{path}[0]", "");
                    meta.Add
                    (
                        await TagHandler.PrepareAttribute(context, aName, value, 1)
                    );
                    for (int i = 1; i < children.Count; i++)
                    {
                        value += $"<br/>{Environment.NewLine}{children[0].First.Path.Replace($"{path}[{i}].", "")}";
                        meta.Add
                        (
                            await TagHandler.PrepareAttribute(context, aName, children[0].First.Path.Replace($"{path}[{i}].", ""), i + 1)
                        );
                    }


                }
                return value;
            }
            catch
            {
                return "";
            }
        }



        /// <summary>
        ///  import jobs
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, ImportJob[] Jobs)>> GetImportJobs(PagingParameterModel paging)
        {
            try
            {
                var source =
                     _context.ImportJobs
                     .Select(j => new ImportJob() { Id = j.Id, Artifact = j.Artifact, ArtifactId = j.ArtifactId, EndTime = j.EndTime, Exception = j.Exception, ProgressPercent = j.ProgressPercent, QueueTime = j.QueueTime, FriendlyUrl = j.FriendlyUrl, JobType = j.JobType, ResourceNumber = j.ResourceNumber, SrcContent = "--omitted--", SrcUrl = j.SrcUrl, StartTime = j.StartTime, Status = j.Status })
                    .OrderByDescending(t => t.QueueTime)
                    .AsQueryable();

                (PaginationMetadata PagingMeta, ImportJob[] Items) paginatedResult =
                    await QueryablePaginator<ImportJob>.Paginate(source, paging);



                return new RServiceResult<(PaginationMetadata PagingMeta, ImportJob[] Items)>(paginatedResult);
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, ImportJob[] Items)>((PagingMeta: null, Items: null), exp.ToString());
            }
        }

        /// <summary>
        /// Bookmark Artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserBookmark>> BookmarkArtifact(Guid artifactId, Guid userId, RBookmarkType type)
        {
            try
            {
                if ((await _context.UserBookmarks.Where(b => b.RAppUserId == userId && b.RArtifactMasterRecordId == artifactId && b.RBookmarkType == type).SingleOrDefaultAsync()) != null)
                {
                    return new RServiceResult<RUserBookmark>(null, "Artifact is already bookmarked/faved.");
                }

                RUserBookmark bookmark =
                    new RUserBookmark()
                    {
                        RAppUserId = userId,
                        RArtifactMasterRecordId = artifactId,
                        RBookmarkType = type,
                        DateTime = DateTime.Now,
                        Note = ""
                    };

                _context.UserBookmarks.Add(bookmark);
                await _context.SaveChangesAsync();

                return new RServiceResult<RUserBookmark>(bookmark);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserBookmark>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get artifact user bookmarks
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserBookmark[]>> GetArtifactUserBookmarks(Guid artifactId, Guid userId)
        {
            try
            {
                RUserBookmark[] bookmarks = await _context.UserBookmarks.Where(b => b.RArtifactMasterRecordId == artifactId && b.RAppUserId == userId).ToArrayAsync();
                return new RServiceResult<RUserBookmark[]>(bookmarks);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserBookmark[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Bookmark Item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserBookmark>> BookmarkItem(Guid itemId, Guid userId, RBookmarkType type)
        {
            try
            {
                if((await _context.UserBookmarks.Where(b => b.RAppUserId == userId && b.RArtifactItemRecordId == itemId && b.RBookmarkType == type).SingleOrDefaultAsync()) != null)
                {
                    return new RServiceResult<RUserBookmark>(null, "Item is already bookmarked/faved.");
                }

                RUserBookmark bookmark =
                    new RUserBookmark()
                    {
                        RAppUserId = userId,
                        RArtifactItemRecordId = itemId,
                        RBookmarkType = type,
                        DateTime = DateTime.Now,
                        Note = ""
                    };

                _context.UserBookmarks.Add(bookmark);
                await _context.SaveChangesAsync();

                return new RServiceResult<RUserBookmark>(bookmark);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserBookmark>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get item user bookmarks
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserBookmark[]>> GeItemUserBookmarks(Guid itemId, Guid userId)
        {
            try
            {
                RUserBookmark[] bookmarks = await _context.UserBookmarks.Where(b => b.RArtifactItemRecordId == itemId && b.RAppUserId == userId).ToArrayAsync();
                return new RServiceResult<RUserBookmark[]>(bookmarks);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserBookmark[]>(null, exp.ToString());
            }
        }
       
        /// <summary>
        /// update bookmark note
        /// </summary>
        /// <param name="bookmarkId"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateUserBookmark(Guid bookmarkId, string note)
        {
            try
            {
                RUserBookmark bookmark =  await _context.UserBookmarks.Where(b => b.Id == bookmarkId).SingleOrDefaultAsync();
                if(bookmark == null)
                {
                    return new RServiceResult<bool>(false, "bookmark not found");
                }
                bookmark.Note = note;
                _context.UserBookmarks.Update(bookmark);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete user bookmark         
        /// /// </summary>
        /// <param name="bookmarkId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteUserBookmark(Guid bookmarkId)
        {
            try
            {
                RUserBookmark bookmark = await _context.UserBookmarks.Where(b => b.Id == bookmarkId).SingleOrDefaultAsync();
                if (bookmark == null)
                {
                    return new RServiceResult<bool>(false, "bookmark not found");
                }
                _context.UserBookmarks.Remove(bookmark);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get user bookmarks (artifacts and items)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RUserBookmarkViewModel[] Bookmarks)>> GetBookmarks(PagingParameterModel paging, Guid userId, RBookmarkType type, PublishStatus[] statusArray)
        {
            try
            {
                var source =
                     _context.UserBookmarks
                     .Include(b =>  b.RArtifactMasterRecord).ThenInclude(a => a.CoverImage)
                     .Include(b => b.RArtifactItemRecord).ThenInclude(b => b.Images)
                     .Where(b => b.RAppUserId == userId && b.RBookmarkType == type)
                    .OrderByDescending(b => b.DateTime)
                    .AsQueryable();

                (PaginationMetadata PagingMeta, RUserBookmark[] Bookmarks) paginatedResult1 =
                    await QueryablePaginator<RUserBookmark>.Paginate(source, paging);


                List<RUserBookmarkViewModel> finalList = new List<RUserBookmarkViewModel>();
                foreach(RUserBookmark bookmark in paginatedResult1.Bookmarks)
                {
                    RUserBookmarkViewModel model = new RUserBookmarkViewModel()
                    {
                        Id = bookmark.Id,
                        RAppUserId = bookmark.RAppUserId,
                        RArtifactMasterRecord = bookmark.RArtifactMasterRecord,
                        RArtifactItemRecord = null,//this should be filled by an external call              
                        DateTime = bookmark.DateTime,
                        RBookmarkType = bookmark.RBookmarkType,
                        Note = bookmark.Note
                    };
                    if(bookmark.RArtifactMasterRecord != null)
                    {
                        if (!statusArray.Contains(bookmark.RArtifactMasterRecord.Status))
                            continue; //this may result in paging bugs
                    }
                    if(bookmark.RArtifactItemRecord != null)
                    {
                        RArtifactMasterRecord parent =
                             await _context.Artifacts
                             .Where(a => statusArray.Contains(a.Status) && a.Id == bookmark.RArtifactItemRecord.RArtifactMasterRecordId)
                             .AsNoTracking()
                             .SingleOrDefaultAsync();

                        if (parent == null)
                            continue;

                        model.RArtifactItemRecord = new RArtifactItemRecordViewModel()
                        {
                            Item = bookmark.RArtifactItemRecord
                        };
                        model.RArtifactItemRecord.ParentFriendlyUrl = parent.FriendlyUrl;
                        model.RArtifactItemRecord.ParentName = parent.Name;
                    }                    
                    finalList.Add(model);
                }


                return new RServiceResult<(PaginationMetadata PagingMeta, RUserBookmarkViewModel[] Bookmarks)>((paginatedResult1.PagingMeta, finalList.ToArray()));
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, RUserBookmarkViewModel[] Bookmarks)>((PagingMeta: null, Bookmarks: null), exp.ToString());
            }
        }

        /// <summary>
        /// Add Note to Artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <param name="noteContents"></param>
        /// <param name="referenceNoteId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNoteViewModel>> AddUserNoteToArtifact(Guid artifactId, Guid userId, RNoteType type, string noteContents, Guid? referenceNoteId)
        {
            try
            {

                RServiceResult<PublicRAppUser> userInfo = await _userService.GetUserInformation(userId);
                if (!string.IsNullOrEmpty(userInfo.ExceptionString))
                    return new RServiceResult<RUserNoteViewModel>(null, userInfo.ExceptionString);

                RUserNote note =
                    new RUserNote()
                    {
                        RAppUserId = userId,
                        RArtifactMasterRecordId = artifactId,
                        NoteType = type,
                        HtmlContent = noteContents,
                        DateTime = DateTime.Now,
                        LastModified = DateTime.Now,
                        Modified = false,
                        Status = PublishStatus.Published,
                        ReferenceNoteId = referenceNoteId
                    };

                _context.UserNotes.Add(note);
                await _context.SaveChangesAsync(); //we need note.Id for the next step

                if (referenceNoteId != null)
                {
                    RUserNote referenceNote = await _context.UserNotes.Where(n => n.Id == referenceNoteId).SingleOrDefaultAsync();
                    if(referenceNote == null)
                    {
                        return new RServiceResult<RUserNoteViewModel>(null, "Reference note not found!");
                    }

                    if(referenceNote.RAppUserId != userId)
                    {
                        RArtifactMasterRecord artificat = await _context.Artifacts.Where(a => a.Id == artifactId).SingleOrDefaultAsync();
                        await _notificationService.PushNotification
                            (
                            referenceNote.RAppUserId,
                            $"پاسخگویی {userInfo.Result.FirstName} {userInfo.Result.SureName} به یادداشت شما دربارهٔ {artificat.Name}",
                            $"برای مشاهدهٔ پاسخ ارائه شده <a href=\"/items/{artificat.FriendlyUrl}#{note.Id}\">اینجا</a> را ببینید.<br />" +
                            $"پاسخ داده شده: <br />" +
                            $"<blockquote cite=\"/item/{artificat.FriendlyUrl}#{note.Id}\">{note.HtmlContent}</blockquote><br />" +
                            $"یادداشت شما: <br />" +
                            $"<blockquote cite=\"/item/{artificat.FriendlyUrl}#{referenceNote.Id}\">{referenceNote.HtmlContent}</blockquote>"
                            );                       
                    }
                }         
                
                return new RServiceResult<RUserNoteViewModel>
                    (
                    new RUserNoteViewModel()
                    {
                        Id = note.Id,
                        RAppUserId = note.RAppUserId,
                        UserName = userInfo.Result.FirstName + " " + userInfo.Result.SureName,
                        RUserImageId = userInfo.Result.RImageId,
                        Modified = note.Modified,
                        NoteType = note.NoteType,
                        HtmlContent = note.HtmlContent,
                        ReferenceNoteId = note.ReferenceNoteId,
                        Status = note.Status,
                        Notes = new RUserNoteViewModel[] { },
                        DateTime = RUserNoteViewModel.PrepareNoteDateTime(note.DateTime),
                        LastModified = RUserNoteViewModel.PrepareNoteDateTime(note.LastModified)
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNoteViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Add Note to Artifact Item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <param name="noteContents"></param>
        /// <param name="referenceNoteId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNoteViewModel>> AddUserNoteToArtifactItem(Guid itemId, Guid userId, RNoteType type, string noteContents, Guid? referenceNoteId)
        {
            try
            {

                RServiceResult<PublicRAppUser> userInfo = await _userService.GetUserInformation(userId);
                if (!string.IsNullOrEmpty(userInfo.ExceptionString))
                    return new RServiceResult<RUserNoteViewModel>(null, userInfo.ExceptionString);

                RUserNote note =
                    new RUserNote()
                    {
                        RAppUserId = userId,
                        RArtifactItemRecordId = itemId,
                        NoteType = type,
                        HtmlContent = noteContents,
                        DateTime = DateTime.Now,
                        LastModified = DateTime.Now,
                        Modified = false,
                        Status = PublishStatus.Published,
                        ReferenceNoteId = referenceNoteId
                    };

                _context.UserNotes.Add(note);
                await _context.SaveChangesAsync();//we need note.Id for the next step

                if (referenceNoteId != null)
                {
                    RUserNote referenceNote = await _context.UserNotes.Where(n => n.Id == referenceNoteId).SingleOrDefaultAsync();
                    if (referenceNote == null)
                    {
                        return new RServiceResult<RUserNoteViewModel>(null, "Reference note not found!");
                    }

                    if (referenceNote.RAppUserId != userId)
                    {
                        RArtifactItemRecord item = await _context.Items.Where(a => a.Id == itemId).SingleOrDefaultAsync();
                        RArtifactMasterRecord artificat = await _context.Artifacts.Where(a => a.Id == item.RArtifactMasterRecordId).SingleOrDefaultAsync();
                        await _notificationService.PushNotification
                            (
                            referenceNote.RAppUserId,
                            $"پاسخگویی {userInfo.Result.FirstName} {userInfo.Result.SureName} به یادداشت شما دربارهٔ {artificat.Name} « {item.Name}",
                            $"برای مشاهدهٔ پاسخ ارائه شده <a href=\"/items/{artificat.FriendlyUrl}/{item.FriendlyUrl}#{note.Id}\">اینجا</a> را ببینید.<br />" +
                            $"پاسخ داده شده: <br />" +
                            $"<blockquote cite=\"/item/{artificat.FriendlyUrl}/{item.FriendlyUrl}#{note.Id}\">{note.HtmlContent}</blockquote><br />" +
                            $"یادداشت شما: <br />" +
                            $"<blockquote cite=\"/item/{artificat.FriendlyUrl}/{item.FriendlyUrl}#{referenceNote.Id}\">{referenceNote.HtmlContent}</blockquote>"
                            );
                    }
                }
               

                return new RServiceResult<RUserNoteViewModel>
                    (
                    new RUserNoteViewModel()
                    {
                        Id = note.Id,
                        RAppUserId = note.RAppUserId,
                        UserName = userInfo.Result.FirstName + " " + userInfo.Result.SureName,
                        RUserImageId = userInfo.Result.RImageId,
                        Modified = note.Modified,
                        NoteType = note.NoteType,
                        HtmlContent = note.HtmlContent,
                        ReferenceNoteId = note.ReferenceNoteId,
                        Status = note.Status,
                        Notes = new RUserNoteViewModel[] { },
                        DateTime = RUserNoteViewModel.PrepareNoteDateTime(note.DateTime),
                        LastModified = RUserNoteViewModel.PrepareNoteDateTime(note.LastModified)
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNoteViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Edit Note
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="userId"></param>
        /// <param name="noteContents"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNoteViewModel>> EditUserNote(Guid noteId, Guid? userId, string noteContents)
        {
            try
            {
                RUserNote note = await _context.UserNotes.
                    Where(n => n.Id == noteId).
                    SingleOrDefaultAsync();
                if (note == null)
                {
                    return new RServiceResult<RUserNoteViewModel>(null, "Note not found.");
                }

                if(userId != null) //sending null here means user is a moderator
                {
                    if (userId != note.RAppUserId)
                    {
                        return new RServiceResult<RUserNoteViewModel>(null, "Note only can be edited by its owner");
                    }
                }

               

                RServiceResult<PublicRAppUser> userInfo = await _userService.GetUserInformation(note.RAppUserId);
                if (!string.IsNullOrEmpty(userInfo.ExceptionString))
                    return new RServiceResult<RUserNoteViewModel>(null, userInfo.ExceptionString);

                

                note.HtmlContent = noteContents;
                note.LastModified = DateTime.Now;

                _context.UserNotes.Update(note);
                await _context.SaveChangesAsync();
                

                return new RServiceResult<RUserNoteViewModel>
                    (
                    new RUserNoteViewModel()
                    {
                        Id = note.Id,
                        RAppUserId = note.RAppUserId,
                        UserName = userInfo.Result.FirstName + " " + userInfo.Result.SureName,
                        RUserImageId = userInfo.Result.RImageId,
                        Modified = note.Modified,
                        NoteType = note.NoteType,
                        HtmlContent = note.HtmlContent,
                        ReferenceNoteId = note.ReferenceNoteId,
                        Status = note.Status,
                        Notes = new RUserNoteViewModel[] { },
                        DateTime = RUserNoteViewModel.PrepareNoteDateTime(note.DateTime),
                        LastModified = RUserNoteViewModel.PrepareNoteDateTime(note.LastModified)
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNoteViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// delete note
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="userId">sending null here means user is a moderator or the note is being deleted in a recursive delete of referenced notes</param>
        /// <returns>list of notes deleted</returns>
        public async Task<RServiceResult<Guid[]>> DeleteUserNote(Guid noteId, Guid? userId)
        {
            try
            {
                RUserNote note = await _context.UserNotes.
                    Where(n => n.Id == noteId).
                    SingleOrDefaultAsync();
                if (note == null)
                {
                    return new RServiceResult<Guid[]>(null, "Note not found.");
                }

                if (userId != null) //sending null here means user is a moderator or the note is being deleted in a recursive delete of referenced notes
                {
                    if (userId != note.RAppUserId)
                    {
                        return new RServiceResult<Guid[]>(null, "Note only can be deleted by its owner");
                    }
                }

                List<Guid> deletedNotesIdSet = new List<Guid>();

                RUserNote[] relatedNotes =  await _context.UserNotes.Where(n => n.ReferenceNoteId == noteId).ToArrayAsync();
                foreach(RUserNote relatedNote in relatedNotes)
                {
                    deletedNotesIdSet.Add(relatedNote.Id);

                    RServiceResult<Guid[]> refDel = await DeleteUserNote(relatedNote.Id, null);
                    if (!string.IsNullOrEmpty(refDel.ExceptionString))
                        return new RServiceResult<Guid[]>(null, refDel.ExceptionString);                    
                }

                deletedNotesIdSet.Add(noteId);

                _context.UserNotes.Remove(note);
                await _context.SaveChangesAsync();

                return new RServiceResult<Guid[]>(deletedNotesIdSet.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<Guid[]>(null, exp.ToString());
            }
        }       


        /// <summary>
        /// get artifact private user notes
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNoteViewModel[]>> GetArtifactUserNotes(Guid artifactId, Guid userId)
        {
            try
            {
                RUserNote[] notes = await _context.UserNotes.
                    Where(b => b.RArtifactMasterRecordId == artifactId && b.RAppUserId == userId && b.NoteType == RNoteType.Private).
                    OrderBy(n => n.DateTime).ToArrayAsync();

                RServiceResult<PublicRAppUser> userInfo = await _userService.GetUserInformation(userId);
                if (!string.IsNullOrEmpty(userInfo.ExceptionString))
                    return new RServiceResult<RUserNoteViewModel[]>(null, userInfo.ExceptionString);

                List<RUserNoteViewModel> res = new List<RUserNoteViewModel>();
                foreach (RUserNote note in notes)
                    res.Add
                        (
                        new RUserNoteViewModel()
                        {
                            Id = note.Id,
                            RAppUserId = note.RAppUserId,
                            UserName = userInfo.Result.FirstName + " " + userInfo.Result.SureName,
                            RUserImageId = userInfo.Result.RImageId,
                            Modified = note.Modified,
                            NoteType = note.NoteType,
                            HtmlContent = note.HtmlContent,
                            ReferenceNoteId = note.ReferenceNoteId,
                            Status = note.Status,
                            Notes = new RUserNoteViewModel[] { },
                            DateTime = RUserNoteViewModel.PrepareNoteDateTime(note.DateTime),
                            LastModified = RUserNoteViewModel.PrepareNoteDateTime(note.LastModified)
                        }
                        );

                return new RServiceResult<RUserNoteViewModel[]>(res.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNoteViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// recursvie call to build notes tree
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="parentNoteId"></param>
        /// <returns></returns>
        private async Task<RUserNoteViewModel[]> _GetArtifactPublicNotes(Guid artifactId, Guid? parentNoteId)
        {
            RUserNote[] notes = await _context.UserNotes.
                   Where(b => b.RArtifactMasterRecordId == artifactId && b.NoteType == RNoteType.Public && b.ReferenceNoteId == parentNoteId)
                   .Include(n => n.RAppUser)
                   .OrderBy(n => n.DateTime).ToArrayAsync();

            List<RUserNoteViewModel> res = new List<RUserNoteViewModel>();
            foreach (RUserNote note in notes)
            {
                RAppUser appUser = note.RAppUser;

                var user =
                    new PublicRAppUser()
                    {
                        Id = appUser.Id,
                        Username = appUser.UserName,
                        Email = appUser.Email,
                        FirstName = appUser.FirstName,
                        SureName = appUser.SureName,
                        PhoneNumber = appUser.PhoneNumber,
                        RImageId = appUser.RImageId,
                        Status = appUser.Status
                    };

                RUserNoteViewModel viewModel
                    =
                    new RUserNoteViewModel()
                    {
                        Id = note.Id,
                        RAppUserId = note.RAppUserId,
                        UserName = user.FirstName + " " + user.SureName,
                        RUserImageId = user.RImageId,
                        Modified = note.Modified,
                        NoteType = note.NoteType,
                        HtmlContent = note.HtmlContent,
                        ReferenceNoteId = note.ReferenceNoteId,
                        Status = note.Status,
                        Notes = new RUserNoteViewModel[] { },
                        DateTime = RUserNoteViewModel.PrepareNoteDateTime(note.DateTime),
                        LastModified = RUserNoteViewModel.PrepareNoteDateTime(note.LastModified)
                    };
                    
                viewModel.Notes = await _GetArtifactPublicNotes(artifactId, note.Id);
                res.Add(viewModel);
            }
            return res.ToArray();
        }

        /// <summary>
        /// get artifact public user notes
        /// </summary>
        /// <param name="artifactId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNoteViewModel[]>> GetArtifactPublicNotes(Guid artifactId)
        {
            try
            {             
                return new RServiceResult<RUserNoteViewModel[]>(await _GetArtifactPublicNotes(artifactId, null));
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNoteViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get item artifact private user notes
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNoteViewModel[]>> GetArtifactItemUserNotes(Guid itemId, Guid userId)
        {
            try
            {
                RUserNote[] notes = await _context.UserNotes.
                    Where(b => b.RArtifactItemRecordId == itemId && b.RAppUserId == userId && b.NoteType == RNoteType.Private).
                    OrderBy(n => n.DateTime).ToArrayAsync();

                RServiceResult<PublicRAppUser> userInfo = await _userService.GetUserInformation(userId);
                if (!string.IsNullOrEmpty(userInfo.ExceptionString))
                    return new RServiceResult<RUserNoteViewModel[]>(null, userInfo.ExceptionString);

                List<RUserNoteViewModel> res = new List<RUserNoteViewModel>();
                foreach (RUserNote note in notes)
                    res.Add
                        (
                        new RUserNoteViewModel()
                        {
                            Id = note.Id,
                            RAppUserId = note.RAppUserId,
                            UserName = userInfo.Result.FirstName + " " + userInfo.Result.SureName,
                            RUserImageId = userInfo.Result.RImageId,
                            Modified = note.Modified,
                            NoteType = note.NoteType,
                            HtmlContent = note.HtmlContent,
                            ReferenceNoteId = note.ReferenceNoteId,
                            Status = note.Status,
                            Notes = new RUserNoteViewModel[] { },
                            DateTime = RUserNoteViewModel.PrepareNoteDateTime(note.DateTime),
                            LastModified = RUserNoteViewModel.PrepareNoteDateTime(note.LastModified)
                        }
                        );

                return new RServiceResult<RUserNoteViewModel[]>(res.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNoteViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// recursvie call to build notes tree for iten
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="parentNoteId"></param>
        /// <returns></returns>
        private async Task<RUserNoteViewModel[]> _GetArtifactItemPublicNotes(Guid itemId, Guid? parentNoteId)
        {
            RUserNote[] notes = await _context.UserNotes.
                    Where(b => b.RArtifactItemRecordId == itemId && b.NoteType == RNoteType.Public && b.ReferenceNoteId == parentNoteId)
                    .Include(n => n.RAppUser)
                    .OrderBy(n => n.DateTime).ToArrayAsync();

            List<RUserNoteViewModel> res = new List<RUserNoteViewModel>();
            foreach (RUserNote note in notes)
            {
                RAppUser appUser = note.RAppUser;

                var user =
                    new PublicRAppUser()
                    {
                        Id = appUser.Id,
                        Username = appUser.UserName,
                        Email = appUser.Email,
                        FirstName = appUser.FirstName,
                        SureName = appUser.SureName,
                        PhoneNumber = appUser.PhoneNumber,
                        RImageId = appUser.RImageId,
                        Status = appUser.Status
                    };

                RUserNoteViewModel viewModel
                    =
                    new RUserNoteViewModel()
                    {
                        Id = note.Id,
                        RAppUserId = note.RAppUserId,
                        UserName = user.FirstName + " " + user.SureName,
                        RUserImageId = user.RImageId,
                        Modified = note.Modified,
                        NoteType = note.NoteType,
                        HtmlContent = note.HtmlContent,
                        ReferenceNoteId = note.ReferenceNoteId,
                        Status = note.Status,
                        Notes = new RUserNoteViewModel[] { },
                        DateTime = RUserNoteViewModel.PrepareNoteDateTime(note.DateTime),
                        LastModified = RUserNoteViewModel.PrepareNoteDateTime(note.LastModified)
                    };
                
                viewModel.Notes = await _GetArtifactItemPublicNotes(itemId, note.Id);
                res.Add(viewModel);
            }
            return res.ToArray();
        }

        /// <summary>
        /// get item artifact item public user notes
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNoteViewModel[]>> GetArtifactItemPublicNotes(Guid itemId)
        {
            try
            {              

                return new RServiceResult<RUserNoteViewModel[]>(await _GetArtifactItemPublicNotes(itemId, null));
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNoteViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get All USer Notes
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="paging"></param>
        /// <param name="type"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>> GetUserNotes(Guid userId, PagingParameterModel paging, RNoteType type, PublishStatus[] statusArray)
        {
            try
            {
                var source =
                     _context.UserNotes
                     .Include(b => b.RArtifactMasterRecord).ThenInclude(a => a.CoverImage)
                     .Include(b => b.RArtifactItemRecord).ThenInclude(b => b.Images)
                     .Where(b => b.RAppUserId == userId && b.NoteType == type)
                    .OrderByDescending(b => b.DateTime)
                    .AsQueryable();

                (PaginationMetadata PagingMeta, RUserNote[] Notes) paginatedResult1 =
                    await QueryablePaginator<RUserNote>.Paginate(source, paging);

                RServiceResult<PublicRAppUser> userInfo = await _userService.GetUserInformation(userId);
                if (!string.IsNullOrEmpty(userInfo.ExceptionString))
                    return new RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>((PagingMeta: null, Notes: null), userInfo.ExceptionString);               

                PublicRAppUser user = userInfo.Result;

                List<RUserNoteViewModel> finalList = new List<RUserNoteViewModel>();
                foreach (RUserNote note in paginatedResult1.Notes)
                {
                    RUserNoteViewModel model = 
                        new RUserNoteViewModel()
                        {
                            Id = note.Id,
                            RAppUserId = note.RAppUserId,
                            UserName = user.FirstName + " " + user.SureName,
                            RUserImageId = user.RImageId,
                            Modified = note.Modified,
                            NoteType = note.NoteType,
                            HtmlContent = note.HtmlContent,
                            ReferenceNoteId = note.ReferenceNoteId,
                            Status = note.Status,
                            Notes = new RUserNoteViewModel[] { },
                            DateTime = RUserNoteViewModel.PrepareNoteDateTime(note.DateTime),
                            LastModified = RUserNoteViewModel.PrepareNoteDateTime(note.LastModified)
                        };
                if (note.RArtifactMasterRecord != null)
                    {
                        if (!statusArray.Contains(note.RArtifactMasterRecord.Status))
                            continue; //this may result in paging bugs
                    }

                    if (note.RArtifactItemRecord != null)
                    {                       

                        RArtifactMasterRecord parent =
                             await _context.Artifacts
                             .Where(a => statusArray.Contains(a.Status) && a.Id == note.RArtifactItemRecord.RArtifactMasterRecordId)
                             .AsNoTracking()
                             .SingleOrDefaultAsync();

                        if (parent == null)
                            continue;

                        model.RelatedEntityName = note.RArtifactItemRecord.Name;
                        model.RelatedEntityImageId = note.RArtifactItemRecord.Images.FirstOrDefault().Id;
                        model.RelatedEntityFriendlyUrl = parent.FriendlyUrl + "/" + note.RArtifactItemRecord.FriendlyUrl;
                        model.RelatedItemParentName = parent.Name;
                    }

                    if(note.RArtifactMasterRecord != null)
                    {
                        model.RelatedEntityName = note.RArtifactMasterRecord.Name;
                        model.RelatedEntityImageId = note.RArtifactMasterRecord.CoverImage.Id;
                        model.RelatedEntityFriendlyUrl = note.RArtifactMasterRecord.FriendlyUrl;
                    }
                    finalList.Add(model);
                }


                return new RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>((paginatedResult1.PagingMeta, finalList.ToArray()));
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>((PagingMeta: null, Notes: null), exp.ToString());
            }
        }

        /// <summary>
        /// Get All Public Notes
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>> GetAllPublicNotes(PagingParameterModel paging)
        {
            try
            {
                var source =
                     _context.UserNotes
                     .Include(b => b.RArtifactMasterRecord).ThenInclude(a => a.CoverImage)
                     .Include(b => b.RArtifactItemRecord).ThenInclude(b => b.Images)
                     .Include(n => n.RAppUser)
                     .Where(b => b.NoteType == RNoteType.Public)
                    .OrderByDescending(b => b.DateTime)
                    .AsQueryable();

                (PaginationMetadata PagingMeta, RUserNote[] Notes) paginatedResult1 =
                    await QueryablePaginator<RUserNote>.Paginate(source, paging);

             
               
                List<RUserNoteViewModel> finalList = new List<RUserNoteViewModel>();
                foreach (RUserNote note in paginatedResult1.Notes)
                {
                    RAppUser appUser = note.RAppUser;

                    var user =
                        new PublicRAppUser()
                        {
                            Id = appUser.Id,
                            Username = appUser.UserName,
                            Email = appUser.Email,
                            FirstName = appUser.FirstName,
                            SureName = appUser.SureName,
                            PhoneNumber = appUser.PhoneNumber,
                            RImageId = appUser.RImageId,
                            Status = appUser.Status
                        };

                    RUserNoteViewModel model
                        =
                        new RUserNoteViewModel()
                        {
                            Id = note.Id,
                            RAppUserId = note.RAppUserId,
                            UserName = user.FirstName + " " + user.SureName,
                            RUserImageId = user.RImageId,
                            Modified = note.Modified,
                            NoteType = note.NoteType,
                            HtmlContent = note.HtmlContent,
                            ReferenceNoteId = note.ReferenceNoteId,
                            Status = note.Status,
                            Notes = new RUserNoteViewModel[] { },
                            DateTime = RUserNoteViewModel.PrepareNoteDateTime(note.DateTime),
                            LastModified = RUserNoteViewModel.PrepareNoteDateTime(note.LastModified)
                        };
                    
                    if (note.RArtifactMasterRecord != null)
                    {
                        if (note.RArtifactMasterRecord.Status != PublishStatus.Published)
                            continue; //this may result in paging bugs
                    }

                    if (note.RArtifactItemRecord != null)
                    {

                        RArtifactMasterRecord parent =
                             await _context.Artifacts
                             .Where(a => a.Status == PublishStatus.Published && a.Id == note.RArtifactItemRecord.RArtifactMasterRecordId)
                             .AsNoTracking()
                             .SingleOrDefaultAsync();

                        if (parent == null)
                            continue;

                        model.RelatedEntityName = note.RArtifactItemRecord.Name;
                        model.RelatedEntityImageId = note.RArtifactItemRecord.Images.FirstOrDefault().Id;
                        model.RelatedEntityFriendlyUrl = parent.FriendlyUrl + "/" + note.RArtifactItemRecord.FriendlyUrl;
                        model.RelatedItemParentName = parent.Name;
                    }

                    if (note.RArtifactMasterRecord != null)
                    {
                        model.RelatedEntityName = note.RArtifactMasterRecord.Name;
                        model.RelatedEntityImageId = note.RArtifactMasterRecord.CoverImage.Id;
                        model.RelatedEntityFriendlyUrl = note.RArtifactMasterRecord.FriendlyUrl;
                    }
                    finalList.Add(model);
                }


                return new RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>((paginatedResult1.PagingMeta, finalList.ToArray()));
            }
            catch (Exception exp)
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>((PagingMeta: null, Notes: null), exp.ToString());
            }
        }



        /// <summary>
        /// suggest ganjoor link
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="link"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorLinkViewModel>> SuggestGanjoorLink(Guid userId, LinkSuggestion link)
        {
            try
            {
                RArtifactMasterRecord artifact = await _context.Artifacts.Where(a => a.FriendlyUrl == link.ArtifactFriendlyUrl).SingleOrDefaultAsync();

                GanjoorLink alreaySuggest = 
                await _context.GanjoorLinks.
                    Where(l => l.GanjoorPostId == link.GanjoorPostId && l.ArtifactId == artifact.Id && l.ItemId == link.ItemId && l.ReviewResult != ReviewResult.Rejected)
                    .SingleOrDefaultAsync();
                if (alreaySuggest != null)
                    return new RServiceResult<GanjoorLinkViewModel>(null, "این مورد پیشتر پیشنهاد شده است.");

                GanjoorLink suggestion =
                    new GanjoorLink()
                    {
                        GanjoorPostId = link.GanjoorPostId,
                        GanjoorTitle = link.GanjoorTitle,
                        GanjoorUrl = link.GanjoorUrl,
                        ArtifactId = artifact.Id,
                        ItemId = link.ItemId,
                        SuggestedById = userId,
                        SuggestionDate = DateTime.Now,
                        ReviewResult = ReviewResult.Awaiting
                    };

                _context.GanjoorLinks.Add(suggestion);
                await _context.SaveChangesAsync();

                string entityName, entityFriendlyUrl;
                Guid entityImageId;
                
                if (suggestion.ItemId == null)
                {                    
                    entityName = artifact.Name;
                    entityFriendlyUrl = $"/items/{artifact.FriendlyUrl}";
                    entityImageId = artifact.CoverImageId;
                }
                else
                {
                    RArtifactItemRecord item = await _context.Items.Include(i => i.Images).Where(i => i.Id == suggestion.ItemId).SingleOrDefaultAsync();
                    entityName = artifact.Name + " » " + item.Name;
                    entityFriendlyUrl = $"/items/{artifact.FriendlyUrl}/{item.FriendlyUrl}";
                    entityImageId = item.Images.First().Id;
                }

                GanjoorLinkViewModel viewModel 
                    = new GanjoorLinkViewModel()
                    {
                        Id = suggestion.Id,
                        GanjoorPostId = suggestion.GanjoorPostId,
                        GanjoorUrl = suggestion.GanjoorUrl,
                        GanjoorTitle = suggestion.GanjoorTitle,
                        EntityName = entityName,
                        EntityFriendlyUrl = entityFriendlyUrl,
                        EntityImageId = entityImageId,
                        ReviewResult = suggestion.ReviewResult,
                        Synchronized = suggestion.Synchronized,
                        SuggestedBy = new PublicRAppUser()
                        {
                            Id = suggestion.SuggestedBy.Id,
                            Username = suggestion.SuggestedBy.UserName,
                            Email = suggestion.SuggestedBy.Email,
                            FirstName = suggestion.SuggestedBy.FirstName,
                            SureName = suggestion.SuggestedBy.SureName,
                            PhoneNumber = suggestion.SuggestedBy.PhoneNumber,
                            RImageId = suggestion.SuggestedBy.RImageId,
                            Status = suggestion.SuggestedBy.Status
                        }
                    };
                return new RServiceResult<GanjoorLinkViewModel>(viewModel);
            }
            catch(Exception exp)
            {
                return new RServiceResult<GanjoorLinkViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// get suggested ganjoor links
        /// </summary>
        /// <param name="status"></param>
        /// <param name="notSynced"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorLinkViewModel[]>> GetSuggestedLinks(ReviewResult status, bool notSynced)
        {
            try
            {
                GanjoorLink[] links =
                await _context.GanjoorLinks
                     .Include(l => l.SuggestedBy)
                     .Include(l => l.Artifact)
                     .Include(l => l.Item).ThenInclude(i => i.Images)
                     .Where(l => l.ReviewResult == status && (notSynced == false || !l.Synchronized))
                     .OrderBy( l => l.SuggestionDate)
                     .ToArrayAsync();
                List<GanjoorLinkViewModel> result = new List<GanjoorLinkViewModel>();
                foreach(GanjoorLink link in links)
                {
                    result.Add
                        (
                        new GanjoorLinkViewModel()
                        {
                            Id = link.Id,
                            GanjoorPostId = link.GanjoorPostId,
                            GanjoorUrl = link.GanjoorUrl,
                            GanjoorTitle = link.GanjoorTitle,
                            EntityName = link.Item == null ? link.Artifact.Name : link.Artifact.Name + " » " + link.Item.Name,
                            EntityFriendlyUrl = link.Item == null ? $"/items/{link.Artifact.FriendlyUrl}" : $"/items/{link.Artifact.FriendlyUrl}/{link.Item.FriendlyUrl}",
                            EntityImageId = link.Item == null ? link.Artifact.CoverImageId : link.Item.Images.First().Id,
                            ReviewResult = link.ReviewResult,
                            Synchronized = link.Synchronized,
                            SuggestedBy = new PublicRAppUser()
                            {
                                Id = link.SuggestedBy.Id,
                                Username = link.SuggestedBy.UserName,
                                Email = link.SuggestedBy.Email,
                                FirstName = link.SuggestedBy.FirstName,
                                SureName = link.SuggestedBy.SureName,
                                PhoneNumber = link.SuggestedBy.PhoneNumber,
                                RImageId = link.SuggestedBy.RImageId,
                                Status = link.SuggestedBy.Status
                            }
                        }
                        );
                }
                return new RServiceResult<GanjoorLinkViewModel[]>(result.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<GanjoorLinkViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Review Suggested Link
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="userId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ReviewSuggestedLink(Guid linkId, Guid userId, ReviewResult result)
        {
            try
            {
                GanjoorLink link =
                await _context.GanjoorLinks
                     .Include(l => l.Artifact).ThenInclude(a => a.Tags).ThenInclude(t => t.RTag)
                     .Include(l => l.Item).ThenInclude(i => i.Tags).ThenInclude(t => t.RTag)
                     .Where(l => l.Id == linkId)
                     .SingleOrDefaultAsync();
                
                link.ReviewResult = result;
                link.ReviewerId = userId;
                link.ReviewDate = DateTime.Now;

                _context.GanjoorLinks.Update(link);

                if(link.ReviewResult == ReviewResult.Approved)
                {
                    RTagValue tag = await TagHandler.PrepareAttribute(_context, "Ganjoor Link", link.GanjoorTitle, 1);
                    tag.ValueSupplement = link.GanjoorUrl;
                    if(link.Item == null)
                    {
                        link.Artifact.Tags.Add(tag);
                        _context.Artifacts.Update(link.Artifact);
                    }
                    else
                    {
                        link.Item.Tags.Add(tag);
                        _context.Items.Update(link.Item);
                    }

                    //add TOC:
                    using (var client = new HttpClient())
                    {
                        using (var httpResult = await client.GetAsync(link.GanjoorUrl))
                        {
                            if (httpResult.IsSuccessStatusCode)
                            {
                                string html = await httpResult.Content.ReadAsStringAsync();
                                int nIndexFirstParagraphOpenning = html.IndexOf("<p>");
                                if (nIndexFirstParagraphOpenning != -1)
                                {
                                    int nIndexFirstParagraphClosing = html.IndexOf("</p>");
                                    if (nIndexFirstParagraphClosing > nIndexFirstParagraphOpenning)
                                    {
                                        string paragraphContent = html.Substring(nIndexFirstParagraphOpenning + "<p>".Length, 1 + nIndexFirstParagraphClosing - (nIndexFirstParagraphOpenning + "</p>".Length));
                                        if (paragraphContent.Length != 0)
                                        {
                                            int openIndex = paragraphContent.IndexOf('<');
                                            while (paragraphContent.Length > 0 && (openIndex != -1))
                                            {
                                                string afterClosing = "";
                                                int closeIndex = paragraphContent.IndexOf('>', openIndex);
                                                if (closeIndex != -1)
                                                    afterClosing = paragraphContent.Substring(closeIndex + 1);
                                                paragraphContent = paragraphContent.Substring(0, openIndex) + afterClosing;
                                                openIndex = paragraphContent.IndexOf('<');
                                            }
                                            paragraphContent = paragraphContent.Replace("<", "").Replace(">", "");
                                            if (paragraphContent.Length > 100)
                                                paragraphContent = paragraphContent.Substring(0, 100) + " ...";
                                            RTagValue toc = await TagHandler.PrepareAttribute(_context, "Title in TOC", paragraphContent, 1);
                                            toc.ValueSupplement = "1";//font size
                                            if (link.Item == null)
                                            {
                                                if (link.Artifact.Tags.Where(t => t.RTag.Name == "Title in TOC" && t.Value == toc.Value).Count() == 0)
                                                {
                                                    toc.Order = 1 + link.Artifact.Tags.Where(t => t.RTag.NameInEnglish == "Title in TOC").Count();
                                                    link.Artifact.Tags.Add(toc);
                                                    _context.Artifacts.Update(link.Artifact);
                                                }
                                            }
                                            else
                                            {
                                                if (link.Item.Tags.Where(t => t.RTag.Name == "Title in TOC" && t.Value == toc.Value).Count() == 0)
                                                {
                                                    toc.Order = 1 + link.Item.Tags.Where(t => t.RTag.NameInEnglish == "Title in TOC").Count();
                                                    link.Item.Tags.Add(toc);
                                                    _context.Items.Update(link.Item);
                                                }

                                            }
                                        }                                            
                                        
                                    }
                                }
                            }

                        }
                    }
                }

                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Temporary api
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<string[]>> AddTOCForSuggestedLinks()
        {
            try
            {
                GanjoorLink[] links =
                await _context.GanjoorLinks
                     .Include(l => l.Artifact).ThenInclude(a => a.Tags).ThenInclude(t => t.RTag)
                     .Include(l => l.Item).ThenInclude(i => i.Tags).ThenInclude(t => t.RTag)
                     .Where(l => l.ReviewResult == ReviewResult.Approved)
                     .ToArrayAsync();

                List<string> lst = new List<string>();

                foreach(GanjoorLink link in links)
                {
                    using(var client = new HttpClient())
                    {
                        using (var result = await client.GetAsync(link.GanjoorUrl))
                        {
                            if (result.IsSuccessStatusCode)
                            {
                                string html = await result.Content.ReadAsStringAsync();
                                int nIndexFirstParagraphOpenning = html.IndexOf("<p>");
                                if(nIndexFirstParagraphOpenning != -1)
                                {
                                    int nIndexFirstParagraphClosing = html.IndexOf("</p>");
                                    if(nIndexFirstParagraphClosing > nIndexFirstParagraphOpenning)
                                    {
                                        string paragraphContent = html.Substring(nIndexFirstParagraphOpenning + "<p>".Length, 1 + nIndexFirstParagraphClosing - (nIndexFirstParagraphOpenning + "</p>".Length));
                                        if (paragraphContent.Length == 0)
                                            continue;
                                        if (paragraphContent.Length > 100)
                                            paragraphContent = paragraphContent.Substring(0, 100) + " ...";
                                        lst.Add(paragraphContent);
                                        RTagValue tag = await TagHandler.PrepareAttribute(_context, "Title in TOC", paragraphContent, 1);
                                        tag.ValueSupplement = "1";//font size
                                        if (link.Item == null)
                                        {
                                            if(link.Artifact.Tags.Where(t => t.RTag.Name == "Title in TOC" && t.Value == tag.Value).Count() == 0)
                                            {
                                                tag.Order = 1 + link.Artifact.Tags.Where(t => t.RTag.NameInEnglish == "Title in TOC").Count();
                                                link.Artifact.Tags.Add(tag);
                                                _context.Artifacts.Update(link.Artifact);
                                            }                                            
                                        }
                                        else
                                        {
                                            if (link.Item.Tags.Where(t => t.RTag.Name == "Title in TOC" && t.Value == tag.Value).Count() == 0)
                                            {
                                                tag.Order = 1 + link.Item.Tags.Where(t => t.RTag.NameInEnglish == "Title in TOC").Count();
                                                link.Item.Tags.Add(tag);
                                                _context.Items.Update(link.Item);
                                            }
                                            
                                        }
                                    }
                                }
                            }                           

                        }
                    }
                }

                

                await _context.SaveChangesAsync();

                return new RServiceResult<string[]>(lst.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<string[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Synchronize suggested link
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SynchronizeSuggestedLink(Guid linkId)
        {
            try
            {
                GanjoorLink link =
                await _context.GanjoorLinks
                     .Where(l => l.Id == linkId)
                     .SingleOrDefaultAsync();

                link.Synchronized = true;

                _context.GanjoorLinks.Update(link);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// get suggested pinterest links
        /// </summary>
        /// <param name="status"></param>
        /// <param name="notSynced"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PinterestLinkViewModel[]>> GetSuggestedPinterestLinks(ReviewResult status, bool notSynced)
        {
            try
            {
                PinterestLink[] links =
                await _context.PinterestLinks
                     .Include(l => l.Artifact)
                     .Include(l => l.Item).ThenInclude(i => i.Images)
                     .Where(l => l.ReviewResult == status && (notSynced == false || !l.Synchronized))
                     .OrderBy(l => l.SuggestionDate)
                     .ToArrayAsync();
                List<PinterestLinkViewModel> result = new List<PinterestLinkViewModel>();
                foreach (PinterestLink link in links)
                {

                    result.Add
                         (
                        new PinterestLinkViewModel()
                        {
                            Id = link.Id,
                            GanjoorPostId = link.GanjoorPostId,
                            GanjoorUrl = link.GanjoorUrl,
                            GanjoorTitle = link.GanjoorTitle,
                            AltText = link.AltText,
                            LinkType = link.LinkType,
                            PinterestImageUrl = link.PinterestImageUrl,
                            PinterestUrl = link.PinterestUrl,
                            SuggestionDate = link.SuggestionDate,
                            ReviewerId = link.ReviewerId,
                            ReviewDate = link.ReviewDate,
                            ReviewResult = link.ReviewResult,
                            ReviewDesc = link.ReviewDesc,
                            ArtifactId = link.ArtifactId,
                            ItemId = link.ItemId,
                            Synchronized = link.Synchronized,
                            EntityName = link.Artifact == null ? null : link.Artifact.Name + " » " + link.Item.Name,
                            EntityFriendlyUrl = link.Artifact == null ? null : $"/items/{link.Artifact.FriendlyUrl}/{link.Item.FriendlyUrl}",
                            EntityImageId = link.Item == null ? Guid.Empty : link.Item.Images.First().Id
                        }
                        );
                }
                return new RServiceResult<PinterestLinkViewModel[]>(result.ToArray());
            }
            catch (Exception exp)
            {
                return new RServiceResult<PinterestLinkViewModel[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// suggest pinterest link
        /// </summary>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PinterestLinkViewModel>> SuggestPinterestLink(PinterestSuggestion suggestion)
        {
            try
            {

                PinterestLink existingLink = 
                await _context.PinterestLinks.Where
                    (
                    p =>
                    p.GanjoorPostId == suggestion.GanjoorPostId
                    &&
                    p.PinterestUrl == suggestion.PinterestUrl
                    &&
                    (p.ReviewResult == ReviewResult.Approved || p.ReviewResult == ReviewResult.Awaiting)
                    ).FirstOrDefaultAsync();
                if (existingLink != null)
                    return new RServiceResult<PinterestLinkViewModel>(null, "این مورد پیشتر پیشنهاد شده است.");
                    
                PinterestLink link = new PinterestLink()
                {
                    GanjoorPostId = suggestion.GanjoorPostId,
                    GanjoorTitle = suggestion.GanjoorTitle,
                    GanjoorUrl = suggestion.GanjoorUrl,
                    AltText = suggestion.AltText,
                    LinkType = suggestion.LinkType,
                    PinterestUrl = suggestion.PinterestUrl,
                    PinterestImageUrl = suggestion.PinterestImageUrl,
                    ReviewResult = ReviewResult.Awaiting,
                    SuggestionDate = DateTime.Now,
                    Synchronized = false
                };
                _context.PinterestLinks.Add(link);
                await _context.SaveChangesAsync();
                return new RServiceResult<PinterestLinkViewModel>
                    (
                    new PinterestLinkViewModel()
                    {
                        Id = link.Id,
                        GanjoorPostId = link.GanjoorPostId,
                        GanjoorUrl = link.GanjoorUrl,
                        GanjoorTitle = link.GanjoorTitle,
                        AltText = link.AltText,
                        LinkType = link.LinkType,
                        PinterestImageUrl = link.PinterestImageUrl,
                        PinterestUrl = link.PinterestUrl,
                        SuggestionDate = link.SuggestionDate,
                        ReviewerId = link.ReviewerId,
                        ReviewDate = link.ReviewDate,
                        ReviewResult = link.ReviewResult,
                        ReviewDesc = link.ReviewDesc,
                        ArtifactId = link.ArtifactId,
                        ItemId = link.ItemId,
                        Synchronized = link.Synchronized,
                        EntityName = null,
                        EntityFriendlyUrl = null,
                        EntityImageId = Guid.Empty
                    }
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<PinterestLinkViewModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Review Suggested Pinterest Link
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="userId"></param>
        /// <param name="altText"></param>
        /// <param name="result"></param>
        /// <param name="reviewDesc"></param>
        /// <param name="imageUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> ReviewSuggestedPinterestLink(Guid linkId, Guid userId, string altText, ReviewResult result, string reviewDesc, string imageUrl)
        {
            try
            {
                PinterestLink link =
                await _context.PinterestLinks
                     .Where(l => l.Id == linkId)
                     .SingleOrDefaultAsync();
                
                link.ReviewResult = result;
                link.ReviewerId = userId;
                link.ReviewDate = DateTime.Now;
                if(!string.IsNullOrEmpty(altText))
                    link.AltText = altText;
                if (!string.IsNullOrEmpty(imageUrl))
                    link.PinterestImageUrl = imageUrl;
                link.ReviewDesc = reviewDesc;

                if (link.ReviewResult == ReviewResult.Approved)
                {
                    PinterestLink relatedArtifactLink =
                    await _context.PinterestLinks
                        .Where(p => p.PinterestUrl == link.PinterestUrl && p.PinterestImageUrl == link.PinterestImageUrl
                                                   && link.Id != p.Id
                                                   && p.ReviewResult == ReviewResult.Approved)
                        .FirstOrDefaultAsync();
                    string titleInToc = link.AltText;
                    if (titleInToc.IndexOfAny(new char[] { '\n', '\r' }) != -1)
                    {
                        string[] lines = titleInToc.Replace('\n', '\r').Split('\r', StringSplitOptions.RemoveEmptyEntries);
                        titleInToc = lines[0];
                    }
                    if (relatedArtifactLink != null)
                    {
                        
                        link.ArtifactId = relatedArtifactLink.ArtifactId;
                        link.ItemId = relatedArtifactLink.ItemId;

                        //TODO: append tags to this existing artifact and its items

                        
                    }
                    else
                    {
                        using (var client = new HttpClient())
                        {
                            var imageResult = await client.GetAsync(link.PinterestImageUrl);


                            int _ImportRetryCount = 5;
                            int _ImportRetryInitialSleep = 500;
                            int retryCount = 0;
                            while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                            {
                                imageResult.Dispose();
                                Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                imageResult = await client.GetAsync(link.PinterestImageUrl);
                                retryCount++;
                            }

                            if (imageResult.IsSuccessStatusCode)
                            {
                                using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                {
                                    string friendlyUrl = Guid.NewGuid().ToString();
                                    string fileName = friendlyUrl + ".jpg";
                                    while((await _context.PictureFiles.Where(p => p.OriginalFileName == fileName).FirstOrDefaultAsync()) != null)
                                    {
                                        fileName = Guid.NewGuid() + "-" + friendlyUrl + ".jpg";
                                    }
                                    RServiceResult<RPictureFile> picture = await _pictureFileService.Add(link.GanjoorTitle, link.AltText, 1, null, link.PinterestUrl, imageStream, fileName, "Pinterest");
                                    if (picture.Result == null)
                                    {
                                        return new RServiceResult<bool>(false, $"_pictureFileService.Add : {picture.ExceptionString}");
                                    }

                                    

                                    RArtifactMasterRecord book = new RArtifactMasterRecord(titleInToc, titleInToc)
                                    {
                                        Status = PublishStatus.Published,
                                        DateTime = DateTime.Now,
                                        LastModified = DateTime.Now,
                                        CoverItemIndex = 0,
                                        FriendlyUrl = friendlyUrl
                                    };

                                    List<RTagValue> meta = new List<RTagValue>();

                                    RTagValue tag = await TagHandler.PrepareAttribute(_context, "Title", titleInToc, 1);
                                    meta.Add(tag);

                                    tag = await TagHandler.PrepareAttribute(_context, "Description", link.AltText, 1);
                                    meta.Add(tag);

                                    tag = await TagHandler.PrepareAttribute(_context, "Source", link.LinkType == LinkType.Pinterest ? "Pinterest" : link.LinkType == LinkType.Instagram ? "Instagram" : link.PinterestUrl, 1);
                                    tag.ValueSupplement = link.PinterestUrl;

                                    meta.Add(tag);

                                    tag = await TagHandler.PrepareAttribute(_context, "Ganjoor Link", link.GanjoorTitle, 1);
                                    tag.ValueSupplement = link.GanjoorUrl;

                                    meta.Add(tag);

                                    tag = await TagHandler.PrepareAttribute(_context, "Title in TOC", titleInToc, 1);
                                    tag.ValueSupplement = "1";//font size

                                    meta.Add(tag);

                                    book.Tags = meta.ToArray();

                                    List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                    int order = 1;

                                    RArtifactItemRecord page = new RArtifactItemRecord()
                                    {
                                        Name = titleInToc,
                                        NameInEnglish = titleInToc,
                                        Description = link.AltText,
                                        DescriptionInEnglish = link.AltText,
                                        Order = order,
                                        FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                        LastModified = DateTime.Now
                                    };

                                    page.Images = new RPictureFile[] { picture.Result };
                                    page.CoverImageIndex = 0;

                                    page.Tags = meta.ToArray();

                                    book.CoverImage = RPictureFile.Duplicate(picture.Result);

                                    pages.Add(page);

                                    book.Items = pages.ToArray();
                                    book.ItemCount = pages.Count;

                                    await _context.Artifacts.AddAsync(book);
                                    await _context.SaveChangesAsync();

                                    link.ArtifactId = book.Id;
                                    link.ItemId = (await _context.Items.Where(i => i.RArtifactMasterRecordId == link.ArtifactId).FirstOrDefaultAsync()).Id;
                                }
                            }
                            else
                            {
                                return new RServiceResult<bool>(false, $"Http result is not ok (url {link.PinterestImageUrl}");
                            }
                        }
                        
                    }
                }

                _context.PinterestLinks.Update(link);


                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Synchronize suggested pinterest link
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SynchronizeSuggestedPinterestLink(Guid linkId)
        {
            try
            {
                PinterestLink link =
                await _context.PinterestLinks
                     .Where(l => l.Id == linkId)
                     .SingleOrDefaultAsync();

                link.Synchronized = true;

                _context.PinterestLinks.Update(link);
                await _context.SaveChangesAsync();

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }




        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RMuseumDbContext _context;

        /// <summary>
        /// Picture File Service
        /// </summary>
        protected readonly IPictureFileService _pictureFileService;


        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Background Task Queue Instance
        /// </summary>
        protected readonly IBackgroundTaskQueue _backgroundTaskQueue;

        /// <summary>
        /// User Service
        /// </summary>
        protected readonly IAppUserService _userService;

        /// <summary>
        /// Messaging service
        /// </summary>
        protected readonly IRNotificationService _notificationService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="pictureFileService"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="userService"></param>
        /// <param name="notificationService"></param>
        public ArtifactService(RMuseumDbContext context, IConfiguration configuration, IPictureFileService pictureFileService, IBackgroundTaskQueue backgroundTaskQueue, IAppUserService userService, IRNotificationService notificationService)
        {
            _context = context;
            _pictureFileService = pictureFileService;
            Configuration = configuration;
            _backgroundTaskQueue = backgroundTaskQueue;
            _userService = userService;
            _notificationService = notificationService;
        }
    }
}
