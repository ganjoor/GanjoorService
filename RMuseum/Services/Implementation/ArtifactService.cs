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
using DNTPersianUtils.Core;
using FluentFTP;

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

        /// <summary>
        /// get tagged publish artifacts (including CoverImage info but not items or tagibutes info) 
        /// </summary>
        /// <param name="tagUrl"></param>
        /// <param name="valueUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactMasterRecord[]>> GetByTagValue(string tagUrl, string valueUrl, PublishStatus[] statusArray)
        {
            RTag tag =
                        await _context.Tags
                        .Where(a => a.FriendlyUrl == tagUrl)
                    .SingleOrDefaultAsync();
            if (tag == null)
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


        /// <summary>
        /// gets specified artifact info (including CoverImage + images +  tagibutes)
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactMasterRecordViewModel>> GetByFriendlyUrl(string friendlyUrl, PublishStatus[] statusArray)
        {
            RArtifactMasterRecord artifact =
                 await _context.Artifacts
                 .Include(a => a.CoverImage)
                 .Include(a => a.Items).ThenInclude(i => i.Images)
                 .Include(a => a.Items).ThenInclude(i => i.Tags).ThenInclude(t => t.RTag)
                 .Include(a => a.Tags).ThenInclude(t => t.RTag)
                 .Where(a => statusArray.Contains(a.Status) && a.FriendlyUrl == friendlyUrl)
                 .AsNoTracking()
                .SingleOrDefaultAsync();

            if (artifact != null)
            {

                return new RServiceResult<RArtifactMasterRecordViewModel>(RArtifactMasterRecord.ToViewModel(artifact));
            }


            return new RServiceResult<RArtifactMasterRecordViewModel>(null);
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

                foreach (RArtifactItemRecord item in artifact.Items)
                {
                    if (item.Tags.Any(v => v.RTagId == rTag.Id && (string.IsNullOrEmpty(tagValueFriendlyUrl) || v.FriendlyUrl == tagValueFriendlyUrl)))
                    {
                        item.Tags = null;
                        filteredItems.Add(item);
                    }
                }

                artifact.Items = filteredItems;

                return new RServiceResult<RArtifactMasterRecordViewModel>(RArtifactMasterRecord.ToViewModel(artifact));
            }
            return new RServiceResult<RArtifactMasterRecordViewModel>(null);
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
            if (string.IsNullOrEmpty(edited.Name))
            {
                return new RServiceResult<RArtifactMasterRecord>(null, "Name could not be empty.");
            }

            RArtifactMasterRecord item =
                 await _context.Artifacts
                 .Where(a => a.Id == edited.Id)
                .SingleOrDefaultAsync();


            if (item != null)
            {
                if (item.Status != edited.Status)
                {
                    if (!canChangeStatusToAwaiting)
                    {
                        return new RServiceResult<RArtifactMasterRecord>(null, "User should be able to change status to Awaiting to complete this operation.");
                    }

                    if (
                        !
                        (
                        (item.Status == PublishStatus.Draft && edited.Status == PublishStatus.Awaiting)
                        ||
                        (item.Status == PublishStatus.Awaiting && edited.Status == PublishStatus.Draft)
                        )
                        )
                    {
                        if (!canPublish)
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

        /// <summary>
        /// Set Artifact Cover Item Index
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="itemIndex"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SetArtifactCoverItemIndex(Guid artifactId, int itemIndex)
        {
            RArtifactMasterRecord artifact = await _context
                .Artifacts.Where(a => a.Id == artifactId)
                .Include(a => a.Items).ThenInclude(i => i.Images)
                .SingleOrDefaultAsync();
            if (artifact == null)
                return new RServiceResult<bool>(false, "Artifact not found.");

            if (itemIndex == artifact.CoverItemIndex)
                return new RServiceResult<bool>(true);

            if (itemIndex < 0 || itemIndex >= artifact.Items.Count())
                return new RServiceResult<bool>(false, "Item not found.");

            artifact.CoverItemIndex = itemIndex;
            artifact.CoverImage = RPictureFile.Duplicate(artifact.Items.ToArray()[itemIndex].Images.First());

            artifact.LastModified = DateTime.Now;

            _context.Artifacts.Update(artifact);
            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);
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
                if (string.IsNullOrEmpty(friendlyUrl))
                    return new RServiceResult<RTagBundleViewModel>(null, "friendlyUrl is null");
                RTag tag =
                     await _context.Tags
                     .Where(a => a.FriendlyUrl == friendlyUrl)
                     .AsNoTracking()
                    .SingleOrDefaultAsync();

                if (tag == null)
                {
                    return new RServiceResult<RTagBundleViewModel>(null, "tag is null");
                }

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
                            }
                            )
                            .OrderByDescending(g => g.Count)
                            .ToArrayAsync()
                            );

                    foreach (var val in values)
                    {
                        var firstItem = _context.Artifacts.AsNoTracking().Include(a => a.Tags).Include(a => a.CoverImage).Where(a => a.Status == PublishStatus.Published
                                    && a.Tags.Any(t => t.RTagId == tag.Id && t.Value == val.Name)).FirstOrDefault();
                        if (firstItem != null)
                        {
                            val.ImageId = firstItem.CoverImageId;
                            val.ExternalNormalSizeImageUrl = firstItem.CoverImage == null ? "" : firstItem.CoverImage.ExternalNormalSizeImageUrl;
                        }
                        else
                            val.Count = 0;
                    }


                    viewModel.Values = values.Where(v => v.Count > 0).ToArray();

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
            return new RServiceResult<DateTime>(await _context.Artifacts.MaxAsync(a => a.LastModified));
        }

        /// <summary>
        /// get all  tags 
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RTag[] Items)>> GetAllTags(PagingParameterModel paging)
        {
            var source =
                 _context.Tags
                .OrderByDescending(t => t.Order)
                .AsQueryable();
            (PaginationMetadata PagingMeta, RTag[] Items) paginatedResult =
                await QueryablePaginator<RTag>.Paginate(source, paging);
            return new RServiceResult<(PaginationMetadata PagingMeta, RTag[] Items)>(paginatedResult);
        }

        /// <summary>
        /// get tag bu friendly url
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTag>> GetTagByFriendlyUrl(string friendlyUrl)
        {
            return new RServiceResult<RTag>(await _context.Tags.Where(t => t.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync());
        }

        /// <summary>
        /// get tag value by frindly url
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagValue>> GetTagValueByFriendlyUrl(Guid tagId, string friendlyUrl)
        {
            RTagValue value = await _context.TagValues.Where(t => t.RTagId == tagId && t.FriendlyUrl == friendlyUrl).FirstOrDefaultAsync();
            if (value != null)
                return new RServiceResult<RTagValue>(value);
            return new RServiceResult<RTagValue>(null, "Tag value not found");
        }

        /// <summary>
        /// add tag
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<RTag>> AddTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                return new RServiceResult<RTag>(null, "Name could not be empty.");
            }

            RTag existingTag =
                 await _context.Tags
                 .Where(a => a.Name == tagName || a.NameInEnglish == tagName)
                .SingleOrDefaultAsync();
            if (existingTag != null)
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


        /// <summary>
        /// edit tag
        /// </summary>
        /// <param name="edited"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTag>> EditTag(RTag edited)
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

                await UpdateTaggedItems(tag);

                await _context.SaveChangesAsync();
            }
            return new RServiceResult<RTag>(tag);
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
            RArtifactMasterRecord artifact =
                 await _context.Artifacts
                 .Include(a => a.Tags).ThenInclude(t => t.RTag)
                 .Where(a => a.Id == artifactId)
                 .AsNoTracking()
                .SingleOrDefaultAsync();

            if (artifact == null)
            {
                return new RServiceResult<Guid?>(null, "artifact not found");
            }

            RArtifactMasterRecordViewModel viewModel = RArtifactMasterRecord.ToViewModel(artifact); //tags are sorted in this method

            int tagOrder = viewModel.ArtifactTags.Where(tag => tag.Id == tagId).FirstOrDefault().Order;
            RArtifactTagViewModel otherTagViewModel;
            if (up)
            {
                otherTagViewModel = viewModel.ArtifactTags.Where(tag => tag.Order < tagOrder).OrderByDescending(tag => tag.Order).FirstOrDefault();
            }
            else
            {
                otherTagViewModel = viewModel.ArtifactTags.Where(tag => tag.Order > tagOrder).OrderBy(tag => tag.Order).FirstOrDefault();
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


        /// <summary>
        /// changes order of tags based on their position in artifact items
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="itemId"></param>
        /// <param name="up">true => up, false => down</param>
        /// <returns>the other tag which its Order has been changed</returns>
        public async Task<RServiceResult<Guid?>> EditTagOrderBasedOnItem(Guid tagId, Guid itemId, bool up)
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


        /// <summary>
        /// get tag value bundle by frindly url
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="valueUrl"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactTagViewModel>> GetTagValueBundleByFiendlyUrl(string friendlyUrl, string valueUrl)
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

        /// <summary>
        /// add artifact tag value
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="rTag"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagValue>> TagArtifact(Guid artifactId, RTag rTag)
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

        /// <summary>
        /// remove artfiact tag value
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UnTagArtifact(Guid artifactId, Guid tagValueId)
        {
            RArtifactMasterRecord item = await _context.Artifacts.Include(i => i.Tags).Where(i => i.Id == artifactId).SingleOrDefaultAsync();
            item.Tags.Remove(item.Tags.Where(t => t.Id == tagValueId).SingleOrDefault());
            item.LastModified = DateTime.Now;
            _context.Artifacts.Update(item);
            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);
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
            if (string.IsNullOrEmpty(edited.Value))
            {
                return new RServiceResult<RTagValue>(null, "Value could not be empty.");
            }

            RArtifactMasterRecord artifact =
                await _context.Artifacts
                 .Include(a => a.Tags)
                 .Where(a => a.Id == artifactId)
                .SingleOrDefaultAsync();
            if (artifact == null)
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
                    artifact.LastModified = DateTime.Now;
                    _context.Update(artifact);
                    await _context.SaveChangesAsync();
                }
            }
            return new RServiceResult<RTagValue>(tag);
        }


        /// <summary>
        /// add item tag value
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="rTag"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RTagValue>> TagItem(Guid itemId, RTag rTag)
        {
            RTag type = await _context.Tags.Where(a => a.Id == rTag.Id).SingleOrDefaultAsync();

            RArtifactItemRecord item = await _context.Items.Include(i => i.Tags).Where(i => i.Id == itemId).SingleOrDefaultAsync();

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

        /// <summary>
        /// remove item tag value
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UnTagItem(Guid itemId, Guid tagValueId)
        {
            RArtifactItemRecord item = await _context.Items.Include(i => i.Tags).Where(i => i.Id == itemId).SingleOrDefaultAsync();
            item.Tags.Remove(item.Tags.Where(t => t.Id == tagValueId).SingleOrDefault());
            item.LastModified = DateTime.Now;
            _context.Items.Update(item);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
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
            RArtifactMasterRecord artifact =
                 await _context.Artifacts
                 .Include(a => a.Tags).ThenInclude(t => t.RTag)
                 .Where(a => a.Id == artifactId)
                .SingleOrDefaultAsync();

            if (artifact == null)
            {
                return new RServiceResult<Guid?>(null, "artifact not found");
            }

            RArtifactMasterRecordViewModel viewModel = RArtifactMasterRecord.ToViewModel(artifact); //tags are sorted in this method

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

            artifact.LastModified = DateTime.Now;
            _context.Artifacts.Update(artifact);

            await _context.SaveChangesAsync();

            return new RServiceResult<Guid?>((Guid?)value2.Id);
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

        /// <summary>
        /// gets specified artifact item info (including images + tagibutes)
        /// </summary>
        /// <param name="artifactUrl"></param>
        /// <param name="itemUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RArtifactItemRecordViewModel>> GetArtifactItemByFrienlyUrl(string artifactUrl, string itemUrl, PublishStatus[] statusArray)
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

            if (parent.Items != null)
                parent.Items = parent.Items.OrderBy(i => i.Order).ToArray();
            if (parent.Tags != null)
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
                ParentExternalNormalSizeImageUrl = parent.CoverImage.ExternalNormalSizeImageUrl,
                ParentItemCount = parent.Items.Count(),
                NextItemFriendlyUrl = nextItem == null ? "" : nextItem.FriendlyUrl,
                NextItemImageId = nextItem == null ? null : nextItem.Images.First().Id,
                NextItemExternalNormalSizeImageUrl = nextItem == null ? null : nextItem.Images.First().ExternalNormalSizeImageUrl,
                PreviousItemFriendlyUrl = prevItem == null ? "" : prevItem.FriendlyUrl,
                PrevItemImageId = prevItem == null ? null : prevItem.Images.First().Id,
                PrevItemExternalNormalSizeImageUrl = prevItem == null ? null : prevItem.Images.First().ExternalNormalSizeImageUrl,
            };
            return new RServiceResult<RArtifactItemRecordViewModel>(res);
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

        /// <summary>
        /// import from external resources
        /// </summary>
        /// <param name="srcType">pdf/loc/princeton/harvard/qajarwomen/hathitrust/penn/cam/bl/folder/walters/cbl/append</param>
        /// <param name="resourceNumber">119/foldername</param>
        /// <param name="friendlyUrl">golestan-baysonghori/artifact id</param>
        /// <param name="resourcePrefix"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> Import(string srcType, string resourceNumber, string friendlyUrl, string resourcePrefix, bool skipUpload)
        {
            return
                 srcType == "pdf" ?
                 await StartImportingLocalPDFFile(resourceNumber, friendlyUrl, resourcePrefix, skipUpload) :
                 srcType == "princeton" ?
                 await StartImportingFromPrinceton(resourceNumber, friendlyUrl, skipUpload)
                 :
                 srcType == "harvard" ?
                 await StartImportingFromHarvard(resourceNumber, friendlyUrl, skipUpload)
                 :
                  srcType == "qajarwomen" ?
                 await StartImportingFromHarvardDirectly(resourceNumber, friendlyUrl, resourcePrefix, skipUpload)
                 :
                  srcType == "hathitrust" ?
                 await StartImportingFromHathiTrust(resourceNumber, friendlyUrl, skipUpload)
                 :
                 srcType == "penn" ?
                 await StartImportingFromPenLibraries(resourceNumber, friendlyUrl, skipUpload)
                 :
                 srcType == "cam" ?
                 await StartImportingFromCambridge(resourceNumber, friendlyUrl, skipUpload)
                 :
                 srcType == "bl" ?
                 await StartImportingFromBritishLibrary(resourceNumber, friendlyUrl, skipUpload)
                 :
                 srcType == "folder" ?
                 await StartImportingFromServerFolder(resourceNumber, friendlyUrl, resourcePrefix, skipUpload)
                 :
                 srcType == "append" ?
                 await StartAppendingFromServerFolder(resourceNumber, Guid.Parse(friendlyUrl), skipUpload)
                 :
                 srcType == "walters" ?
                 await StartImportingFromWalters(resourceNumber, friendlyUrl, skipUpload)
                  :
                 srcType == "cbl" ?
                 await StartImportingFromChesterBeatty(resourceNumber, friendlyUrl, skipUpload)
                 :
                 await StartImportingFromTheLibraryOfCongress(resourceNumber, friendlyUrl, resourcePrefix, skipUpload);
        }

        /// <summary>
        /// upload artifact to external server
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        public RServiceResult<bool> StartUploadingArtifactToExternalServer(Guid artifactId, bool skipUpload)
        {
            try
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem
                           (
                           async token =>
                           {
                               using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                               {
                                   LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                                   var job = (await jobProgressServiceEF.NewJob($"StartUploadingArtifactToExternalServer : {artifactId}", "Query data")).Result;

                                   try
                                   {
                                       var book = await context.Artifacts.Include(a => a.Items).ThenInclude(i => i.Images).Include(a => a.CoverImage).SingleAsync();
                                       await jobProgressServiceEF.UpdateJob(job.Id, 2, $"بارگذاری {book.Name}");
                                       var resUpload = await _UploadArtifactToExternalServer(book, context, skipUpload);
                                       if(!string.IsNullOrEmpty(resUpload.ExceptionString))
                                       {
                                           await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, resUpload.ExceptionString);
                                       }
                                       if(!resUpload.Result)
                                       {
                                           await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "_UploadArtifactToExternalServer returned false");
                                       }
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, succeeded: true);
                                   }
                                   catch (Exception exp)
                                   {
                                       await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                                   }

                               }
                           });
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// upload artifact to external server
        /// </summary>
        /// <param name="book"></param>
        /// <param name="context"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> _UploadArtifactToExternalServer(RArtifactMasterRecord book, RMuseumDbContext context, bool skipUpload)
        {
            LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
            var job = (await jobProgressServiceEF.NewJob("_UploadArtifactToExternalServer", $"Uploading {book.Name}")).Result;

            try
            {
                if (bool.Parse(Configuration.GetSection("ExternalFTPServer")["UploadEnabled"]))
                {
                    var ftpClient = new AsyncFtpClient
                    (
                        Configuration.GetSection("ExternalFTPServer")["Host"],
                        Configuration.GetSection("ExternalFTPServer")["Username"],
                        Configuration.GetSection("ExternalFTPServer")["Password"]
                    );

                   

                    if (!skipUpload)
                    {
                        ftpClient.ValidateCertificate += FtpClient_ValidateCertificate;
                        await ftpClient.AutoConnect();
                        ftpClient.Config.RetryAttempts = 3;
                    }
                   

                    foreach (var imageSizeString in new string[] { "orig", "norm", "thumb" })
                    {
                        var localFilePath = _pictureFileService.GetImagePath(book.CoverImage, imageSizeString).Result;
                        if (imageSizeString == "orig")
                        {
                            book.CoverImage.ExternalNormalSizeImageUrl = $"{Configuration.GetSection("ExternalFTPServer")["RootUrl"]}/{book.CoverImage.FolderName}/orig/{Path.GetFileName(localFilePath)}";
                            context.Update(book.CoverImage);
                        }
                        if(!skipUpload)
                        {
                            await jobProgressServiceEF.UpdateJob(job.Id, 0, localFilePath);

                            var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/{book.CoverImage.FolderName}/{imageSizeString}/{Path.GetFileName(localFilePath)}";
                            await ftpClient.UploadFile(localFilePath, remoteFilePath, createRemoteDir: true);
                        }
                      
                    }


                    foreach (var item in book.Items)
                    {
                        foreach (var image in item.Images)
                        {
                            foreach (var imageSizeString in new string[] { "orig", "norm", "thumb" })
                            {
                                var localFilePath = _pictureFileService.GetImagePath(image, imageSizeString).Result;
                                if (imageSizeString == "orig")
                                {
                                    image.ExternalNormalSizeImageUrl = $"{Configuration.GetSection("ExternalFTPServer")["RootUrl"]}/{image.FolderName}/orig/{Path.GetFileName(localFilePath)}";
                                    context.Update(image);
                                }
                                if(!skipUpload)
                                {
                                    await jobProgressServiceEF.UpdateJob(job.Id, 0, localFilePath);
                                    var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/{book.CoverImage.FolderName}/{imageSizeString}/{Path.GetFileName(localFilePath)}";
                                    await ftpClient.UploadFile(localFilePath, remoteFilePath, createRemoteDir: true);
                                }
                               
                            }
                        }
                    }

                    if(!skipUpload)
                    {
                        await ftpClient.Disconnect();
                    }
                    await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                    await context.SaveChangesAsync();//redundant
                }

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// reschedule jobs
        /// </summary>
        /// <param name="jobType"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RescheduleJobs(JobType jobType, bool skipUpload)
        {

            ImportJob[] jobs = await _context.ImportJobs.Where(j => j.Status != ImportJobStatus.Succeeded && j.JobType == jobType).OrderByDescending(j => j.ProgressPercent).ToArrayAsync();

            List<string> scheduled = new List<string>();

            foreach (ImportJob job in jobs)
            {
                if (job.Status != ImportJobStatus.Failed)
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

                    RServiceResult<bool> rescheduled =
                        job.JobType == JobType.Pdf ?
                        await StartImportingLocalPDFFile(job.ResourceNumber, job.FriendlyUrl, job.SrcUrl, skipUpload)
                        :
                        job.JobType == JobType.Princeton ?
                 await StartImportingFromPrinceton(job.ResourceNumber, job.FriendlyUrl, skipUpload)
                 :
                 job.JobType == JobType.Harvard ?
                 await StartImportingFromHarvard(job.ResourceNumber, job.FriendlyUrl, skipUpload)
                 :
                  job.JobType == JobType.HarvardDirect ?
                 await StartImportingFromHarvardDirectly(job.ResourceNumber, job.FriendlyUrl, job.SrcUrl, skipUpload)
                 :
                  job.JobType == JobType.HathiTrust ?
                 await StartImportingFromHathiTrust(job.ResourceNumber, job.FriendlyUrl, skipUpload)
                 :
                 job.JobType == JobType.PennLibraries ?
                 await StartImportingFromPenLibraries(job.ResourceNumber, job.FriendlyUrl, skipUpload)
                 :
                 job.JobType == JobType.Cambridge ?
                 await StartImportingFromCambridge(job.ResourceNumber, job.FriendlyUrl, skipUpload)
                 :
                 job.JobType == JobType.BritishLibrary ?
                 await StartImportingFromBritishLibrary(job.ResourceNumber, job.FriendlyUrl, skipUpload)
                 :
                 job.JobType == JobType.ServerFolder ?
                 await StartImportingFromServerFolder(job.ResourceNumber, job.FriendlyUrl, job.SrcUrl, skipUpload)
                 :
                 job.JobType == JobType.Walters ?
                 await StartImportingFromWalters(job.ResourceNumber, job.FriendlyUrl, skipUpload)
                  :
                 job.JobType == JobType.ChesterBeatty ?
                 await StartImportingFromChesterBeatty(job.ResourceNumber, job.FriendlyUrl, skipUpload)
                 :
                 new RServiceResult<bool>(false, "StartImportingFromTheLibraryOfCongress NOT SUPPORTED");

                    if (rescheduled.Result)
                    {
                        _context.ImportJobs.Remove(job);
                        await _context.SaveChangesAsync();
                    }

                }
            }

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// an incomplete prototype for removing artifacts
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="checkJobs"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> RemoveArtifact(Guid artifactId, bool checkJobs)
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

                if (await _context.UserBookmarks.Where(b => b.RArtifactMasterRecordId == artifactId).AnyAsync())
                {
                    var bookmarks = await _context.UserBookmarks.Where(b => b.RArtifactMasterRecordId == artifactId).ToListAsync();
                    _context.RemoveRange(bookmarks);
                }
                if (await _context.UserNotes.Where(n => n.RArtifactMasterRecordId == artifactId).AnyAsync())
                {
                    var notes = await _context.UserNotes.Where(n => n.RArtifactMasterRecordId == artifactId).ToListAsync();
                    _context.RemoveRange(notes);
                }

                if (await _context.GanjoorLinks.Where(l => l.ArtifactId == artifactId).AnyAsync())
                {
                    var links = await _context.GanjoorLinks.Where(l => l.ArtifactId == artifactId).ToListAsync();
                    _context.RemoveRange(links);
                }

                if (checkJobs)
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

                string artifactFolder = "";
                if (record.Items.Count > 0)
                {
                    var firstImage = record.Items.First();
                    if (firstImage.Images.Count > 0)
                    {
                        artifactFolder = Path.Combine(_pictureFileService.ImageStoragePath, firstImage.Images.First().FolderName);
                    }

                }


                foreach (RArtifactItemRecord item in record.Items)
                {
                    var bookmarks = await _context.UserBookmarks.Where(b => b.RArtifactItemRecordId == item.Id).ToListAsync();
                    _context.RemoveRange(bookmarks);

                    var notes = await _context.UserNotes.Where(n => n.RArtifactItemRecordId == artifactId).ToListAsync();
                    _context.RemoveRange(notes);

                    _context.PictureFiles.RemoveRange(item.Images);
                    _context.TagValues.RemoveRange(item.Tags);
                }

                _context.Items.RemoveRange(record.Items);
                _context.TagValues.RemoveRange(record.Tags);
                _context.Artifacts.Remove(record);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(artifactFolder))
                {
                    try
                    {
                        Directory.Delete(artifactFolder, true);
                    }
                    catch
                    {
                        //ignore errprs
                    }
                }
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
            return new RServiceResult<bool>(true);
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
            var source =
                 _context.ImportJobs
                 .Select(j => new ImportJob() { Id = j.Id, Artifact = j.Artifact, ArtifactId = j.ArtifactId, EndTime = j.EndTime, Exception = j.Exception, ProgressPercent = j.ProgressPercent, QueueTime = j.QueueTime, FriendlyUrl = j.FriendlyUrl, JobType = j.JobType, ResourceNumber = j.ResourceNumber, SrcContent = "--omitted--", SrcUrl = j.SrcUrl, StartTime = j.StartTime, Status = j.Status })
                .OrderByDescending(t => t.QueueTime)
                .AsQueryable();
            (PaginationMetadata PagingMeta, ImportJob[] Items) paginatedResult =
                await QueryablePaginator<ImportJob>.Paginate(source, paging);

            return new RServiceResult<(PaginationMetadata PagingMeta, ImportJob[] Items)>(paginatedResult);
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

        /// <summary>
        /// get artifact user bookmarks
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserBookmark[]>> GetArtifactUserBookmarks(Guid artifactId, Guid userId)
        {
            RUserBookmark[] bookmarks = await _context.UserBookmarks.Where(b => b.RArtifactMasterRecordId == artifactId && b.RAppUserId == userId).ToArrayAsync();
            return new RServiceResult<RUserBookmark[]>(bookmarks);
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
            if ((await _context.UserBookmarks.Where(b => b.RAppUserId == userId && b.RArtifactItemRecordId == itemId && b.RBookmarkType == type).SingleOrDefaultAsync()) != null)
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

        /// <summary>
        /// get item user bookmarks
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserBookmark[]>> GeItemUserBookmarks(Guid itemId, Guid userId)
        {
            RUserBookmark[] bookmarks = await _context.UserBookmarks.Where(b => b.RArtifactItemRecordId == itemId && b.RAppUserId == userId).ToArrayAsync();
            return new RServiceResult<RUserBookmark[]>(bookmarks);
        }

        /// <summary>
        /// update bookmark note
        /// </summary>
        /// <param name="bookmarkId"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> UpdateUserBookmark(Guid bookmarkId, string note)
        {
            RUserBookmark bookmark = await _context.UserBookmarks.Where(b => b.Id == bookmarkId).SingleOrDefaultAsync();
            if (bookmark == null)
            {
                return new RServiceResult<bool>(false, "bookmark not found");
            }
            bookmark.Note = note;
            _context.UserBookmarks.Update(bookmark);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// delete user bookmark         
        /// /// </summary>
        /// <param name="bookmarkId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteUserBookmark(Guid bookmarkId)
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
            var source =
                 _context.UserBookmarks
                 .Include(b => b.RArtifactMasterRecord).ThenInclude(a => a.CoverImage)
                 .Include(b => b.RArtifactItemRecord).ThenInclude(b => b.Images)
                 .Where(b => b.RAppUserId == userId && b.RBookmarkType == type)
                .OrderByDescending(b => b.DateTime)
                .AsQueryable();

            (PaginationMetadata PagingMeta, RUserBookmark[] Bookmarks) paginatedResult1 =
                await QueryablePaginator<RUserBookmark>.Paginate(source, paging);


            List<RUserBookmarkViewModel> finalList = new List<RUserBookmarkViewModel>();
            foreach (RUserBookmark bookmark in paginatedResult1.Bookmarks)
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
                if (bookmark.RArtifactMasterRecord != null)
                {
                    if (!statusArray.Contains(bookmark.RArtifactMasterRecord.Status))
                        continue; //this may result in paging bugs
                }
                if (bookmark.RArtifactItemRecord != null)
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
                if (referenceNote == null)
                {
                    return new RServiceResult<RUserNoteViewModel>(null, "Reference note not found!");
                }

                if (referenceNote.RAppUserId != userId)
                {
                    RArtifactMasterRecord artificat = await _context.Artifacts.Where(a => a.Id == artifactId).SingleOrDefaultAsync();
                    await _notificationService.PushNotification
                        (
                        referenceNote.RAppUserId,
                        $"پاسخگویی {userInfo.Result.NickName} به یادداشت شما دربارهٔ {artificat.Name}",
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
                    UserName = userInfo.Result.NickName,
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
                        $"پاسخگویی {userInfo.Result.NickName} به یادداشت شما دربارهٔ {artificat.Name} « {item.Name}",
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
                    UserName = userInfo.Result.NickName,
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

        /// <summary>
        /// Edit Note
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="userId"></param>
        /// <param name="noteContents"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNoteViewModel>> EditUserNote(Guid noteId, Guid? userId, string noteContents)
        {
            RUserNote note = await _context.UserNotes.
                Where(n => n.Id == noteId).
                SingleOrDefaultAsync();
            if (note == null)
            {
                return new RServiceResult<RUserNoteViewModel>(null, "Note not found.");
            }

            if (userId != null) //sending null here means user is a moderator
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
                    UserName = userInfo.Result.NickName,
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

        /// <summary>
        /// delete note
        /// </summary>
        /// <param name="noteId"></param>
        /// <param name="userId">sending null here means user is a moderator or the note is being deleted in a recursive delete of referenced notes</param>
        /// <returns>list of notes deleted</returns>
        public async Task<RServiceResult<Guid[]>> DeleteUserNote(Guid noteId, Guid? userId)
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

            RUserNote[] relatedNotes = await _context.UserNotes.Where(n => n.ReferenceNoteId == noteId).ToArrayAsync();
            foreach (RUserNote relatedNote in relatedNotes)
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


        /// <summary>
        /// get artifact private user notes
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNoteViewModel[]>> GetArtifactUserNotes(Guid artifactId, Guid userId)
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
                        UserName = userInfo.Result.NickName,
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
                        Status = appUser.Status,
                        NickName = appUser.NickName,
                        Website = appUser.Website,
                        Bio = appUser.Bio,
                        EmailConfirmed = appUser.EmailConfirmed

                    };

                RUserNoteViewModel viewModel
                    =
                    new RUserNoteViewModel()
                    {
                        Id = note.Id,
                        RAppUserId = note.RAppUserId,
                        UserName = user.NickName,
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
            return new RServiceResult<RUserNoteViewModel[]>(await _GetArtifactPublicNotes(artifactId, null));
        }

        /// <summary>
        /// get item artifact private user notes
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNoteViewModel[]>> GetArtifactItemUserNotes(Guid itemId, Guid userId)
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
                        UserName = userInfo.Result.NickName,
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
                        Status = appUser.Status,
                        NickName = appUser.NickName,
                        Website = appUser.Website,
                        Bio = appUser.Bio,
                        EmailConfirmed = appUser.EmailConfirmed
                    };

                RUserNoteViewModel viewModel
                    =
                    new RUserNoteViewModel()
                    {
                        Id = note.Id,
                        RAppUserId = note.RAppUserId,
                        UserName = user.NickName,
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
            return new RServiceResult<RUserNoteViewModel[]>(await _GetArtifactItemPublicNotes(itemId, null));
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
                        UserName = user.NickName,
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
                    model.RelatedEntityImageId = note.RArtifactItemRecord.Images.First().Id;
                    model.RelatedEntityExternalNormalSizeImageUrl = note.RArtifactItemRecord.Images.First().ExternalNormalSizeImageUrl;
                    model.RelatedEntityFriendlyUrl = parent.FriendlyUrl + "/" + note.RArtifactItemRecord.FriendlyUrl;
                    model.RelatedItemParentName = parent.Name;
                }

                if (note.RArtifactMasterRecord != null)
                {
                    model.RelatedEntityName = note.RArtifactMasterRecord.Name;
                    model.RelatedEntityImageId = note.RArtifactMasterRecord.CoverImage.Id;
                    model.RelatedEntityExternalNormalSizeImageUrl = note.RArtifactMasterRecord.CoverImage.ExternalNormalSizeImageUrl;
                    model.RelatedEntityFriendlyUrl = note.RArtifactMasterRecord.FriendlyUrl;
                }
                finalList.Add(model);
            }


            return new RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>((paginatedResult1.PagingMeta, finalList.ToArray()));
        }

        /// <summary>
        /// Get All Public Notes
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>> GetAllPublicNotes(PagingParameterModel paging)
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
                        Status = appUser.Status,
                        NickName = appUser.NickName,
                        Website = appUser.Website,
                        Bio = appUser.Bio,
                        EmailConfirmed = appUser.EmailConfirmed
                    };

                RUserNoteViewModel model
                    =
                    new RUserNoteViewModel()
                    {
                        Id = note.Id,
                        RAppUserId = note.RAppUserId,
                        UserName = user.NickName,
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
                    model.RelatedEntityImageId = note.RArtifactItemRecord.Images.First().Id;
                    model.RelatedEntityExternalNormalSizeImageUrl = note.RArtifactItemRecord.Images.First().ExternalNormalSizeImageUrl;
                    model.RelatedEntityFriendlyUrl = parent.FriendlyUrl + "/" + note.RArtifactItemRecord.FriendlyUrl;
                    model.RelatedItemParentName = parent.Name;
                }

                if (note.RArtifactMasterRecord != null)
                {
                    model.RelatedEntityName = note.RArtifactMasterRecord.Name;
                    model.RelatedEntityImageId = note.RArtifactMasterRecord.CoverImage.Id;
                    model.RelatedEntityFriendlyUrl = note.RArtifactMasterRecord.FriendlyUrl;
                    model.RelatedEntityExternalNormalSizeImageUrl = note.RArtifactMasterRecord.CoverImage.ExternalNormalSizeImageUrl;
                }
                finalList.Add(model);
            }
            return new RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)>((paginatedResult1.PagingMeta, finalList.ToArray()));
        }

        /// <summary>
        /// report a public note
        /// </summary>
        /// <param name="reportUserId"></param>
        /// <param name="noteId"></param>
        /// <param name="reasonText"></param>
        /// <returns>id of report record</returns>
        public async Task<RServiceResult<Guid>> ReportPublicNote(Guid reportUserId, Guid noteId, string reasonText)
        {
            try
            {
                RUserNoteAbuseReport r = new RUserNoteAbuseReport()
                {
                    NoteId = noteId,
                    ReporterId = reportUserId,
                    ReasonText = reasonText,
                };
                _context.ReportedUserNotes.Add(r);
                await _context.SaveChangesAsync();
                return new RServiceResult<Guid>(r.Id);
            }
            catch (Exception exp)
            {
                return new RServiceResult<Guid>(Guid.Empty, exp.ToString());
            }
        }

        /// <summary>
        /// delete a report for abuse in public user notes
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeclinePublicNoteReport(Guid id)
        {
            try
            {
                RUserNoteAbuseReport report = await _context.ReportedUserNotes.Where(r => r.Id == id).SingleOrDefaultAsync();
                if (report == null)
                {
                    return new RServiceResult<bool>(false);
                }
                _context.ReportedUserNotes.Remove(report);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete a reported user note (accept the complaint)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> AcceptPublicNoteReport(Guid id)
        {
            try
            {
                RUserNoteAbuseReport report = await _context.ReportedUserNotes.AsNoTracking().Where(r => r.Id == id).SingleOrDefaultAsync();
                if (report == null)
                {
                    return new RServiceResult<bool>(false, "report not found");
                }

                var reasonText = report.ReasonText;

                RUserNote note = await _context.UserNotes.Where(n => n.Id == report.NoteId).SingleOrDefaultAsync();
                if (note == null)
                {
                    return new RServiceResult<bool>(false, "note not found!");
                }


                await _notificationService.PushNotification((Guid)note.RAppUserId,
                                       "حذف یادداشت عمومی شما",
                                       $"یادداشت عمومی شما به دلیل ناسازگاری با قوانین یادداشت‌گذاری عمومی در گنجینهٔ گنجور و طبق گزارشات دیگر کاربران حذف شده است.{Environment.NewLine}" +
                                       $"توجه فرمایید که یادداشتهای عمومی گنجینهٔ گنجور برای بحث در مورد نسخه‌ها در نظر گرفته شده‌اند و جای بحثهای محتوایی بی‌ربط به نسخهٔ خاص می‌تواند در گنجور باشد.{Environment.NewLine}" +
                                       $"{reasonText}" +
                                       $"این متن یادداشت حذف شدهٔ شماست: {Environment.NewLine}" +
                                       $"{note.HtmlContent}"
                                       );
                _context.UserNotes.Remove(note);

                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Get a list of reported notes
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RUserNoteAbuseReportViewModel[] Items)>> GetReportedPublicNotes(PagingParameterModel paging)
        {
            var source =
                 from report in _context.ReportedUserNotes
                 join note in _context.UserNotes.Include(n => n.RAppUser)
                 on report.NoteId equals note.Id
                 select
                 new RUserNoteAbuseReportViewModel()
                 {
                     Id = report.Id,
                     ReasonText = report.ReasonText,
                     Note = new RUserNoteViewModel()
                     {
                         Id = note.Id,
                         RAppUserId = note.RAppUserId,
                         UserName = note.RAppUser.NickName,
                         RUserImageId = note.RAppUser.RImageId,
                         Modified = note.Modified,
                         NoteType = note.NoteType,
                         HtmlContent = note.HtmlContent,
                         ReferenceNoteId = note.ReferenceNoteId,
                         Status = note.Status,
                         Notes = new RUserNoteViewModel[] { },
                         DateTime = RUserNoteViewModel.PrepareNoteDateTime(note.DateTime),
                         LastModified = RUserNoteViewModel.PrepareNoteDateTime(note.LastModified)
                     }
                 };

            (PaginationMetadata PagingMeta, RUserNoteAbuseReportViewModel[] Items) paginatedResult =
                await QueryablePaginator<RUserNoteAbuseReportViewModel>.Paginate(source, paging);
            return new RServiceResult<(PaginationMetadata PagingMeta, RUserNoteAbuseReportViewModel[] Items)>(paginatedResult);
        }



        /// <summary>
        /// suggest ganjoor link
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="link"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorLinkViewModel>> SuggestGanjoorLink(Guid userId, LinkSuggestion link)
        {
            RArtifactMasterRecord artifact = await _context.Artifacts.Where(a => a.FriendlyUrl == link.ArtifactFriendlyUrl).SingleOrDefaultAsync();

            GanjoorLink alreadySuggest =
            await _context.GanjoorLinks.
                Where(l => l.GanjoorPostId == link.GanjoorPostId && l.ArtifactId == artifact.Id && l.ItemId == link.ItemId && l.ReviewResult != ReviewResult.Rejected)
                .SingleOrDefaultAsync();
            if (alreadySuggest != null)
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

            var user = (await _userService.GetUserInformation(userId)).Result;

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
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        SureName = user.SureName,
                        PhoneNumber = user.PhoneNumber,
                        RImageId = user.RImageId,
                        Status = user.Status,
                        NickName = user.NickName,
                        Website = user.Website,
                        Bio = user.Bio,
                        EmailConfirmed = user.EmailConfirmed
                    },
                    IsTextOriginalSource = suggestion.IsTextOriginalSource
                };
            return new RServiceResult<GanjoorLinkViewModel>(viewModel);
        }

        /// <summary>
        /// get Unsynchronized image count
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetUnsynchronizedSuggestedLinksCount()
        {
            return new RServiceResult<int>
                (
                  await _context.GanjoorLinks.AsNoTracking()
                 .Include(l => l.SuggestedBy)
                 .Include(l => l.Artifact)
                 .Include(l => l.Item).ThenInclude(i => i.Images)
                 .Where(l => l.ReviewResult == ReviewResult.Approved && !l.Synchronized)
                 .CountAsync()
                 );
        }

        /// <summary>
        /// finds what the method name suggests
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorLinkViewModel[]>> GetNextUnsynchronizedSuggestedLinkWithAlreadySynchedOneForPoem(int skip)
        {
            GanjoorLink link =
            await _context.GanjoorLinks.AsNoTracking()
                 .Include(l => l.SuggestedBy)
                 .Include(l => l.Artifact)
                 .Include(l => l.Item).ThenInclude(i => i.Images)
                 .Where(l => l.ReviewResult == ReviewResult.Approved && !l.Synchronized)
                 .OrderBy(l => l.SuggestionDate)
                 .Skip(skip)
                 .FirstOrDefaultAsync();
            if (link == null)
                return new RServiceResult<GanjoorLinkViewModel[]>(null);
            List<GanjoorLinkViewModel> result = new List<GanjoorLinkViewModel>();
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
                             Status = link.SuggestedBy.Status,
                             NickName = link.SuggestedBy.NickName,
                             Website = link.SuggestedBy.Website,
                             Bio = link.SuggestedBy.Bio,
                             EmailConfirmed = link.SuggestedBy.EmailConfirmed
                         },
                         IsTextOriginalSource = link.IsTextOriginalSource
                     }
                     );
            GanjoorLink preLink =
            await _context.GanjoorLinks.AsNoTracking()
                 .Include(l => l.SuggestedBy)
                 .Include(l => l.Artifact)
                 .Include(l => l.Item).ThenInclude(i => i.Images)
                 .Where(l => l.ReviewResult == ReviewResult.Approved && l.DisplayOnPage && l.GanjoorPostId == link.GanjoorPostId && l.ArtifactId == link.ArtifactId)
                 .OrderBy(l => l.SuggestionDate)
                 .FirstOrDefaultAsync();
            if (preLink != null)
            {
                result.Add
                     (
                     new GanjoorLinkViewModel()
                     {
                         Id = preLink.Id,
                         GanjoorPostId = preLink.GanjoorPostId,
                         GanjoorUrl = preLink.GanjoorUrl,
                         GanjoorTitle = preLink.GanjoorTitle,
                         EntityName = preLink.Item == null ? preLink.Artifact.Name : preLink.Artifact.Name + " » " + preLink.Item.Name,
                         EntityFriendlyUrl = preLink.Item == null ? $"/items/{preLink.Artifact.FriendlyUrl}" : $"/items/{preLink.Artifact.FriendlyUrl}/{preLink.Item.FriendlyUrl}",
                         EntityImageId = preLink.Item == null ? preLink.Artifact.CoverImageId : preLink.Item.Images.First().Id,
                         ReviewResult = preLink.ReviewResult,
                         Synchronized = preLink.Synchronized,
                         SuggestedBy = new PublicRAppUser()
                         {
                             Id = preLink.SuggestedBy.Id,
                             Username = preLink.SuggestedBy.UserName,
                             Email = preLink.SuggestedBy.Email,
                             FirstName = preLink.SuggestedBy.FirstName,
                             SureName = preLink.SuggestedBy.SureName,
                             PhoneNumber = preLink.SuggestedBy.PhoneNumber,
                             RImageId = preLink.SuggestedBy.RImageId,
                             Status = preLink.SuggestedBy.Status,
                             NickName = preLink.SuggestedBy.NickName,
                             Website = preLink.SuggestedBy.Website,
                             Bio = preLink.SuggestedBy.Bio,
                             EmailConfirmed = preLink.SuggestedBy.EmailConfirmed
                         },
                         IsTextOriginalSource = preLink.IsTextOriginalSource
                     }
                     );
            }
            return new RServiceResult<GanjoorLinkViewModel[]>(result.ToArray());
        }


        /// <summary>
        /// get suggested ganjoor links
        /// </summary>
        /// <param name="status"></param>
        /// <param name="notSynced"></param>
        /// <returns></returns>
        public async Task<RServiceResult<GanjoorLinkViewModel[]>> GetSuggestedLinks(ReviewResult status, bool notSynced)
        {
            GanjoorLink[] links =
            await _context.GanjoorLinks.AsNoTracking()
                 .Include(l => l.SuggestedBy)
                 .Include(l => l.Artifact)
                 .Include(l => l.Item).ThenInclude(i => i.Images)
                 .Where(l => l.ReviewResult == status && (notSynced == false || !l.Synchronized))
                 .OrderBy(l => l.SuggestionDate)
                 .ToArrayAsync();
            List<GanjoorLinkViewModel> result = new List<GanjoorLinkViewModel>();
            foreach (GanjoorLink link in links)
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
                            Status = link.SuggestedBy.Status,
                            NickName = link.SuggestedBy.NickName,
                            Website = link.SuggestedBy.Website,
                            Bio = link.SuggestedBy.Bio,
                            EmailConfirmed = link.SuggestedBy.EmailConfirmed
                        },
                        IsTextOriginalSource = link.IsTextOriginalSource
                    }
                    );
            }
            return new RServiceResult<GanjoorLinkViewModel[]>(result.ToArray());
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
            GanjoorLink link =
            await _context.GanjoorLinks
                 .Include(l => l.Artifact).ThenInclude(a => a.Tags).ThenInclude(t => t.RTag)
                 .Include(l => l.Item).ThenInclude(i => i.Tags).ThenInclude(t => t.RTag)
                 .Where(l => l.Id == linkId)
                 .SingleOrDefaultAsync();

            var poem = (await _ganjoorService.GetPoemById(link.GanjoorPostId)).Result;//if it fails here nothing is updated
            string titleInTOC = poem == null ? "" : poem.FullTitle;

            if (poem != null && poem.Verses.Length > 0)
            {
                link.GanjoorTitle = poem.FullTitle;
                titleInTOC += $" - {poem.Verses[0].Text}";
            }

            link.ReviewResult = result;
            link.ReviewerId = userId;
            link.ReviewDate = DateTime.Now;

            //check to see if any other image from this artifact has been added to this poem:
            if (
                link.ReviewResult == ReviewResult.Approved
                &&
                !await _context.GanjoorLinks.Where(l => l.ArtifactId == link.ArtifactId && l.GanjoorPostId == link.GanjoorPostId && l.ReviewResult == ReviewResult.Approved && l.DisplayOnPage).AnyAsync()
                )
            {
                link.DisplayOnPage = true;
                link.Synchronized = true;



                await _ganjoorService.CacheCleanForPageById(link.GanjoorPostId);
            }//if not user must decide through UI for this link

            _context.GanjoorLinks.Update(link);

            if (link.ReviewResult == ReviewResult.Approved)
            {

                var itemInfo = await _context.Items
                                        .Include(i => i.Tags)
                                        .ThenInclude(t => t.RTag)
                                        .Where(i => i.Id == link.ItemId).SingleAsync();

                var sourceTag = itemInfo.Tags.Where(t => t.RTag.FriendlyUrl == "source").FirstOrDefault();

                if (sourceTag != null)
                {
                    if (!string.IsNullOrEmpty(sourceTag.ValueSupplement) && (sourceTag.ValueSupplement.IndexOf("http") == 0))
                    {
                        link.OriginalSourceUrl = sourceTag.ValueSupplement;
                        link.LinkToOriginalSource = false;
                    }
                }

                RTagValue tag = await TagHandler.PrepareAttribute(_context, "Ganjoor Link", link.GanjoorTitle, 1);
                tag.ValueSupplement = link.GanjoorUrl;
                if (link.Item == null)
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



                RTagValue toc = await TagHandler.PrepareAttribute(_context, "Title in TOC", titleInTOC, 1);
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

            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// Temporary api
        /// </summary>
        /// <returns></returns>
        public async Task<RServiceResult<string[]>> AddTOCForSuggestedLinks()
        {
            GanjoorLink[] links =
            await _context.GanjoorLinks
                 .Include(l => l.Artifact).ThenInclude(a => a.Tags).ThenInclude(t => t.RTag)
                 .Include(l => l.Item).ThenInclude(i => i.Tags).ThenInclude(t => t.RTag)
                 .Where(l => l.ReviewResult == ReviewResult.Approved)
                 .ToArrayAsync();

            List<string> lst = new List<string>();

            foreach (GanjoorLink link in links)
            {
                var poem = (await _ganjoorService.GetPoemById(link.GanjoorPostId)).Result;//if it fails here nothing is updated
                string titleInTOC = poem.FullTitle;

                if (poem.Verses.Length > 0)
                {
                    titleInTOC += $" - {poem.Verses[0].Text}";
                }

                RTagValue tag = await TagHandler.PrepareAttribute(_context, "Title in TOC", titleInTOC, 1);
                tag.ValueSupplement = "1";//font size
                if (link.Item == null)
                {
                    if (link.Artifact.Tags.Where(t => t.RTag.Name == "Title in TOC" && t.Value == tag.Value).Count() == 0)
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
            await _context.SaveChangesAsync();

            return new RServiceResult<string[]>(lst.ToArray());
        }

        /// <summary>
        /// Synchronize suggested link
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="displayOnPage"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SynchronizeSuggestedLink(Guid linkId, bool displayOnPage)
        {
            GanjoorLink link =
            await _context.GanjoorLinks
                 .Where(l => l.Id == linkId)
                 .SingleOrDefaultAsync();

            link.Synchronized = true;
            link.DisplayOnPage = displayOnPage;

            _context.GanjoorLinks.Update(link);
            await _context.SaveChangesAsync();

            await _ganjoorService.CacheCleanForPageById(link.GanjoorPostId);

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// get suggested pinterest links
        /// </summary>
        /// <param name="status"></param>
        /// <param name="notSynced"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PinterestLinkViewModel[]>> GetSuggestedPinterestLinks(ReviewResult status, bool notSynced)
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

        /// <summary>
        /// suggest pinterest link
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        public async Task<RServiceResult<PinterestLinkViewModel>> SuggestPinterestLink(Guid userId, PinterestSuggestion suggestion)
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
                SuggestedById = userId,
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
                if (!string.IsNullOrEmpty(altText))
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
                                    while ((await _context.PictureFiles.Where(p => p.OriginalFileName == fileName).FirstOrDefaultAsync()) != null)
                                    {
                                        fileName = Guid.NewGuid() + "-" + friendlyUrl + ".jpg";
                                    }
                                    RServiceResult<RPictureFile> picture = await _pictureFileService.Add(link.GanjoorTitle, link.AltText, 1, null, link.PinterestUrl, imageStream, fileName, "Pinterest");
                                    if (picture.Result == null)
                                    {
                                        return new RServiceResult<bool>(false, $"_pictureFileService.Add : {picture.ExceptionString}");
                                    }

                                    if (bool.Parse(Configuration.GetSection("ExternalFTPServer")["UploadEnabled"]))
                                    {
                                        var ftpClient = new AsyncFtpClient
                                        (
                                            Configuration.GetSection("ExternalFTPServer")["Host"],
                                            Configuration.GetSection("ExternalFTPServer")["Username"],
                                            Configuration.GetSection("ExternalFTPServer")["Password"]
                                        );
                                        ftpClient.ValidateCertificate += FtpClient_ValidateCertificate;
                                        await ftpClient.AutoConnect();
                                        ftpClient.Config.RetryAttempts = 3;
                                        foreach (var imageSizeString in new string[] { "orig", "norm", "thumb" })
                                        {
                                            var localFilePath = _pictureFileService.GetImagePath(picture.Result, imageSizeString).Result;
                                            if (imageSizeString == "orig")
                                            {
                                                picture.Result.ExternalNormalSizeImageUrl = $"{Configuration.GetSection("ExternalFTPServer")["RootUrl"]}/Pinterest/orig/{Path.GetFileName(localFilePath)}";
                                            }
                                            var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/Pinterest/{imageSizeString}/{Path.GetFileName(localFilePath)}";
                                            await ftpClient.UploadFile(localFilePath, remoteFilePath);
                                        }
                                        await ftpClient.Disconnect();
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

        private void FtpClient_ValidateCertificate(FluentFTP.Client.BaseClient.BaseFtpClient control, FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        }

        /// <summary>
        /// Synchronize suggested pinterest link
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> SynchronizeSuggestedPinterestLink(Guid linkId)
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

        /// <summary>
        /// Search Artifacts
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items)>> SearchArtifacts(PagingParameterModel paging, string term)
        {
            term = term.Trim().ApplyCorrectYeKe();

            if (string.IsNullOrEmpty(term))
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items)>((null, null), "خطای جستجوی عبارت خالی");
            }

            term = term.Replace("‌", " ");//replace zwnj with space

            string searchConditions;
            if (term.IndexOf('"') == 0 && term.LastIndexOf('"') == (term.Length - 1))
            {
                searchConditions = term.Replace("\"", "").Replace("'", "");
                searchConditions = $"\"{searchConditions}\"";
            }
            else
            {
                string[] words = term.Replace("\"", "").Replace("'", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

                searchConditions = "";
                string emptyOrAnd = "";
                foreach (string word in words)
                {
                    searchConditions += $" {emptyOrAnd} \"*{word}*\" ";
                    emptyOrAnd = " AND ";
                }
            }

            /*
             CREATE FULLTEXT CATALOG RArtifactMasterRecord

            GO

            CREATE FULLTEXT INDEX ON [dbo].[Artifacts](
            [Name] LANGUAGE 'English',
            [NameInEnglish] LANGUAGE 'English',
            [Description] LANGUAGE 'English',
            [DescriptionInEnglish] LANGUAGE 'English'
            )
            KEY INDEX [PK_Artifacts]ON ([RArtifactMasterRecord], FILEGROUP [PRIMARY])
            WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)

            GO

            CREATE FULLTEXT INDEX ON [dbo].[Items](
            [Name] LANGUAGE 'English',
            [NameInEnglish] LANGUAGE 'English',
            [Description] LANGUAGE 'English',
            [DescriptionInEnglish] LANGUAGE 'English'
            )
            KEY INDEX [PK_Items]ON ([RArtifactMasterRecord], FILEGROUP [PRIMARY])
            WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)

            GO

            CREATE FULLTEXT INDEX ON [dbo].[TagValues](
            [Value] LANGUAGE 'English',
            [ValueInEnglish] LANGUAGE 'English'
            )
            KEY INDEX [PK_TagValues]ON ([RArtifactMasterRecord], FILEGROUP [PRIMARY])
            WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)

             */

            var source =
                _context.Artifacts.AsNoTracking().Include(a => a.Tags).Include(a => a.CoverImage)
                .Where(p =>
                       p.Status == PublishStatus.Published
                       &&
                       (
                       EF.Functions.Contains(p.Name, searchConditions)
                       ||
                       EF.Functions.Contains(p.NameInEnglish, searchConditions)
                       ||
                       EF.Functions.Contains(p.Description, searchConditions)
                       ||
                       EF.Functions.Contains(p.DescriptionInEnglish, searchConditions)
                       ||
                       p.Tags.Where(t => EF.Functions.Contains(t.Value, searchConditions) || EF.Functions.Contains(t.ValueInEnglish, searchConditions)).Any()
                       )
                       ).OrderBy(i => i.Id);


            (PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items) paginatedResult =
               await QueryablePaginator<RArtifactMasterRecord>.Paginate(source, paging);


            return new RServiceResult<(PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items)>(paginatedResult);
        }

        /// <summary>
        /// search artifact items
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        public async Task<RServiceResult<(PaginationMetadata PagingMeta, RArtifactItemRecordViewModel[] Items)>> SearchArtifactItems(PagingParameterModel paging, string term)
        {
            term = term.Trim().ApplyCorrectYeKe();

            if (string.IsNullOrEmpty(term))
            {
                return new RServiceResult<(PaginationMetadata PagingMeta, RArtifactItemRecordViewModel[] Items)>((null, null), "خطای جستجوی عبارت خالی");
            }

            term = term.Replace("‌", " ");//replace zwnj with space

            string searchConditions;
            if (term.IndexOf('"') == 0 && term.LastIndexOf('"') == (term.Length - 1))
            {
                searchConditions = term.Replace("\"", "").Replace("'", "");
                searchConditions = $"\"{searchConditions}\"";
            }
            else
            {
                string[] words = term.Replace("\"", "").Replace("'", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

                searchConditions = "";
                string emptyOrAnd = "";
                foreach (string word in words)
                {
                    searchConditions += $" {emptyOrAnd} \"*{word}*\" ";
                    emptyOrAnd = " AND ";
                }
            }

            var source =
                _context.Items.AsNoTracking().Include(a => a.Tags).Include(a => a.Images)
                .Where(p =>
                        p.Images.Count > 0
                        &&
                       (
                       EF.Functions.Contains(p.Name, searchConditions)
                       ||
                       EF.Functions.Contains(p.NameInEnglish, searchConditions)
                       ||
                       EF.Functions.Contains(p.Description, searchConditions)
                       ||
                       EF.Functions.Contains(p.DescriptionInEnglish, searchConditions)
                       ||
                       p.Tags.Where(t => EF.Functions.Contains(t.Value, searchConditions) || EF.Functions.Contains(t.ValueInEnglish, searchConditions)).Any()
                       )
                       ).OrderBy(i => i.Id);


            (PaginationMetadata PagingMeta, RArtifactItemRecord[] Items) paginatedResult =
               await QueryablePaginator<RArtifactItemRecord>.Paginate(source, paging);

            List<RArtifactItemRecordViewModel> viewModels = new List<RArtifactItemRecordViewModel>();
            foreach (var item in paginatedResult.Items)
            {
                RArtifactItemRecordViewModel model = new RArtifactItemRecordViewModel();
                if (!item.Images.Any())//?! this sometimes happen!
                {
                    item.Images = (await _context.Items.AsNoTracking().Include(i => i.Images).Where(i => i.Id == item.Id).SingleAsync()).Images;
                }
                model.Item = item;
                model.ParentFriendlyUrl = (await _context.Artifacts.AsNoTracking().Where(a => a.Id == item.RArtifactMasterRecordId).SingleAsync()).FriendlyUrl;
                viewModels.Add(model);
            }

            return new RServiceResult<(PaginationMetadata PagingMeta, RArtifactItemRecordViewModel[] Items)>((paginatedResult.PagingMeta, viewModels.ToArray()));
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
        /// Ganjoor Service
        /// </summary>
        private readonly IGanjoorService _ganjoorService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="pictureFileService"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="userService"></param>
        /// <param name="notificationService"></param>
        /// <param name="ganjoorService"></param>
        public ArtifactService(RMuseumDbContext context, IConfiguration configuration, IPictureFileService pictureFileService, IBackgroundTaskQueue backgroundTaskQueue, IAppUserService userService, IRNotificationService notificationService, IGanjoorService ganjoorService)
        {
            _context = context;
            _pictureFileService = pictureFileService;
            Configuration = configuration;
            _backgroundTaskQueue = backgroundTaskQueue;
            _userService = userService;
            _notificationService = notificationService;
            _ganjoorService = ganjoorService;
        }
    }
}
