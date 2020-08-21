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
using RMuseum.Models.Notification;
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
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Drawing;
using System.Drawing.Imaging;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IArtifactService implementation
    /// </summary>
    public class ArtifactService : IArtifactService
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
                    RArtifactTagViewModel viewModel = new RArtifactTagViewModel(tag);
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
        /// from https://www.loc.gov
        /// </summary>
        /// <param name="resourceNumber">
        /// <example>
        /// m084
        /// </example>
        /// </param>
        /// <param name="friendlyUrl">
        /// <example>
        /// boostan1207
        /// </example>
        /// </param>
        /// <param name="resourcePrefix"></param>
        /// <example>
        /// plmp
        /// </example>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromTheLibraryOfCongress(string resourceNumber, string friendlyUrl, string resourcePrefix)
        {
            string url = $"https://www.loc.gov/resource/{resourcePrefix}.{resourceNumber}/?fo=json&st=gallery";

            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.Loc && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                    .SingleOrDefaultAsync()
                )
                !=
                null
                )
            {
                return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing {url}");
            }

            if(string.IsNullOrEmpty(friendlyUrl))
            {
                friendlyUrl = resourceNumber;
            }

            if(
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
               )
            {
                return new RServiceResult<bool>(false, $"duplicated friendly url '{friendlyUrl}'");
            }

            ImportJob job = new ImportJob()
            {
                JobType = JobType.Loc,
                ResourceNumber = resourceNumber,
                FriendlyUrl = friendlyUrl,
                SrcUrl = url,
                QueueTime = DateTime.Now,
                ProgressPercent = 0,
                Status = ImportJobStatus.NotStarted
            };


            await _context.ImportJobs.AddAsync
                (
                job
                );

            await _context.SaveChangesAsync();

            try
            {

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {
                                using(RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration) )
                                {
                                    job.StartTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Running;
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                                int pageCount = 0;
                                int representative_index = 0;
                                //اول یک صفحه را می‌خوانیم تا تعداد صفحات را مشخص کنیم
                                using (var client = new HttpClient())
                                {

                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {
                                            string json = await result.Content.ReadAsStringAsync();
                                            var parsed = JObject.Parse(json);

                                            pageCount = parsed.SelectToken("resource.segment_count").Value<int>();
                                            representative_index = parsed.SelectToken("resource.representative_index").Value<int>();
                                        }
                                        else
                                        {
                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                            {
                                                job.EndTime = DateTime.Now;
                                                job.Status = ImportJobStatus.Failed;
                                                job.Exception = $"Http result is not ok ({result.StatusCode}) for {url}";
                                                importJobUpdaterDb.Update(job);
                                                await importJobUpdaterDb.SaveChangesAsync();
                                            }
                                            return;
                                        }

                                    }
                                }

                                //here might be problems: loc json does not return correct answer when number of segments are more than 1000
                                /*
                                if (pageCount > 1000)
                                {
                                    job.Exception = $"Page count ({pageCount}) was cut to 1000 for this artifact due to loc bug.";
                                    pageCount = 1000;
                                }
                                */

                                //حالا که تعداد صفحات را داریم دوباره می‌خوانیم
                                url = $"https://www.loc.gov/resource/{resourcePrefix}.{resourceNumber}/?c={pageCount}&fo=json&st=gallery";
                                using (var client = new HttpClient())
                                {

                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {

                                            //here is a problem, this method could be called from a background service where _context is disposed, so I need to renew it
                                            using(RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                            {
                                                RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from {url}", $"extracted from {url}")
                                                {
                                                    Status = PublishStatus.Draft,
                                                    DateTime = DateTime.Now,
                                                    LastModified = DateTime.Now,
                                                    CoverItemIndex = representative_index,
                                                    FriendlyUrl = friendlyUrl
                                                };

                                                string json = await result.Content.ReadAsStringAsync();

                                                job.SrcContent = json;

                                                var parsed = JObject.Parse(json);

                                                var segmentsArray = parsed.SelectToken("segments").ToArray();
                                                //here might be problems: loc json does not return correct answer when number of segments are more than 1000
                                                //I've added some temporary solutions prior
                                                //Here I want to log any paradox I encounter:
                                                if(segmentsArray.Length != pageCount)
                                                {
                                                    job.Exception = $"Page count ({pageCount}) is not equal to number of returned resources ({segmentsArray.Length}).";
                                                }



                                                List<RTagValue> meta = new List<RTagValue>();

                                                string string_value = await HandleSimpleValue(context, parsed, meta, "item.title", "Title");
                                                if (!string.IsNullOrWhiteSpace(string_value))
                                                {
                                                    book.Name = string_value;
                                                    book.NameInEnglish = string_value;
                                                }
                                                await HandleSimpleValue(context, parsed, meta, "item.date", "Date");
                                                string_value = await HandleListValue(context, parsed, meta, "item.other_title", "Other Title");
                                                if (!string.IsNullOrWhiteSpace(string_value))
                                                {
                                                    book.Name = string_value;
                                                }
                                                await HandleListValue(context, parsed, meta, "item.contributor_names", "Contributor Names");
                                                await HandleSimpleValue(context, parsed, meta, "item.shelf_id", "Shelf ID");
                                                await HandleListValue(context, parsed, meta, "item.created_published", "Created / Published");
                                                await HandleListValue(context, parsed, meta, "item.subject_headings", "Subject Headings");
                                                await HandleListValue(context, parsed, meta, "item.notes", "Notes");
                                                await HandleListValue(context, parsed, meta, "item.medium", "Medium");
                                                await HandleListValue(context, parsed, meta, "item.call_number", "Call Number/Physical Location");
                                                await HandleListValue(context, parsed, meta, "item.digital_id", "Digital Id");
                                                await HandleSimpleValue(context, parsed, meta, "item.library_of_congress_control_number", "Library of Congress Control Number");
                                                await HandleChildrenValue(context, parsed, meta, "item.language", "Language");
                                                await HandleListValue(context, parsed, meta, "item.online_format", "Online Format");
                                                await HandleListValue(context, parsed, meta, "item.number_oclc", "OCLC Number");
                                                string_value = await HandleListValue(context, parsed, meta, "item.description", "Description");
                                                if(!string.IsNullOrEmpty(string_value))
                                                {
                                                    book.Description = string_value;
                                                    book.DescriptionInEnglish = string_value;
                                                }
                                                await HandleSimpleValue(context, parsed, meta, "cite_this.chicago", "Chicago citation style");
                                                await HandleSimpleValue(context, parsed, meta, "cite_this.apa", "APA citation style");
                                                await HandleSimpleValue(context, parsed, meta, "cite_this.mla", "MLA citation style");
                                                await HandleChildrenValue(context, parsed, meta, "item.dates", "Dates");
                                                await HandleChildrenValue(context, parsed, meta, "item.contributors", "Contributors");
                                                await HandleChildrenValue(context, parsed, meta, "item.location", "Location");
                                                await HandleListValue(context, parsed, meta, "item.rights", "Rights & Access");

                                                RTagValue tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "Library of Congress, African and Middle East Division, Near East Section Persian Manuscript Collection", 1);
                                                tag.ValueSupplement = url;
                                                string_value = parsed.SelectToken("item.id").Value<string>();
                                                if (!string.IsNullOrWhiteSpace(string_value))
                                                {
                                                    tag.ValueSupplement = string_value;
                                                }

                                                meta.Add(tag);


                                                book.Tags = meta.ToArray();




                                                int order = 0;
                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                //due to loc bug for books with more than 1000 pages relying on segmentsArray changed to hard coded image urls and ....
                                                //foreach (JToken segment in segmentsArray)
                                                for(int pageIndex = 1; pageIndex <= pageCount; pageIndex++)
                                                {
                                                    /*
                                                    using (var fakeClient = new HttpClient())
                                                    {

                                                        using (var fakeRes = await fakeClient.GetAsync("https://ganjgah.ir/api/artifacts/keep-alive"))
                                                        {
                                                            if (fakeRes.IsSuccessStatusCode)
                                                            {
                                                                await fakeRes.Content.ReadAsStringAsync();
                                                            }
                                                        }
                                                    }
                                                    */

                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.ProgressPercent = order * 100 / (decimal)pageCount;
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }

                                                    order++;
                                                    


                                                    RArtifactItemRecord page = new RArtifactItemRecord()
                                                    {
                                                        Name = $"تصویر {order}",
                                                        NameInEnglish =  $"Image {pageIndex} of {book.NameInEnglish}",//segment.SelectToken("title").Value<string>(),
                                                        Description = "",
                                                        DescriptionInEnglish = "",
                                                        Order = order,
                                                        FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                                        LastModified = DateTime.Now
                                                    };

                                                    tag =  await TagHandler.PrepareAttribute(context, "Source", "Library of Congress, African and Middle East Division, Near East Section Persian Manuscript Collection", 1);
                                                    tag.ValueSupplement = $"http://www.loc.gov/resource/{resourcePrefix}.{resourceNumber}/?sp={pageIndex}";//segment.SelectToken("id").Value<string>();
                                                    page.Tags = new RTagValue[] { tag };

                                                    string imageUrlPart = $"{pageIndex}".PadLeft(4, '0');
                                                    //string imageUrl = $"https://tile.loc.gov/image-services/iiif/service:amed:{resourcePrefix}:{resourceNumber}:{imageUrlPart}/full/pct:100/0/default.jpg";
                                                    string imageUrl = $"https://tile.loc.gov/image-services/iiif/service:rbc:{resourcePrefix}:2015:{resourceNumber}:{imageUrlPart}/full/pct:100/0/default.jpg";
                                                    /*
                                                    List<string> list = segment.SelectToken("image_url").ToObject<List<string>>();
                                                    if (list != null && list.Count > 0)
                                                    {
                                                        for (int i = 0; i < list.Count; i++)
                                                        {
                                                            if (list[i].IndexOf(".jpg") != -1)
                                                            {
                                                                if (imageUrl == "")
                                                                    imageUrl = list[i];
                                                                else
                                                                {
                                                                    if (imageUrl.IndexOf("#h=") != -1 && imageUrl.IndexOf("&w=", imageUrl.IndexOf("#h=")) != -1)
                                                                    {
                                                                        int h1 = int.Parse(imageUrl.Substring(imageUrl.IndexOf("#h=") + "#h=".Length, imageUrl.IndexOf("&w=") - imageUrl.IndexOf("#h=") - "&w=".Length));
                                                                        if (list[i].IndexOf("#h=") != -1 && list[i].IndexOf("&w=", list[i].IndexOf("#h=")) != -1)
                                                                        {
                                                                            int h2 = int.Parse(list[i].Substring(list[i].IndexOf("#h=") + "#h=".Length, list[i].IndexOf("&w=") - list[i].IndexOf("#h=") - "&w=".Length));

                                                                            if (h2 > h1)
                                                                            {
                                                                                imageUrl = list[i];
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                        imageUrl = list[i];

                                                                }
                                                            }
                                                        }
                                                    }
                                                    */



                                                    if (!string.IsNullOrEmpty(imageUrl))
                                                    {
                                                        //imageUrl = "https:" + imageUrl.Substring(0, imageUrl.IndexOf('#'));
                                                        bool recovered = false;
                                                        if (
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           &&
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           &&
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           )
                                                        {
                                                            RServiceResult<RPictureFile> picture = await _pictureFileService.RecoverFromeFiles(page.Name, page.Description, 1,
                                                                imageUrl, 
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                            if (picture.Result != null)
                                                            {
                                                                recovered = true;
                                                                page.Images = new RPictureFile[] { picture.Result };
                                                                page.CoverImageIndex = 0;

                                                                if (book.CoverItemIndex == (order - 1))
                                                                {
                                                                    book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                }

                                                                pages.Add(page);
                                                            }
                                                            
                                                        }
                                                        if(!recovered)
                                                        {
                                                            if (
                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )
                                                              
                                                           )
                                                            {
                                                                File.Delete
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               );
                                                            }
                                                            if (
                                                               
                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )
                                                               
                                                           )
                                                            {
                                                                File.Delete
                                                                (
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                );
}
                                                            if (
                                                              
                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )
                                                           )
                                                            {
                                                                File.Delete
                                                                (
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                );
                                                            }
                                                            var imageResult = await client.GetAsync(imageUrl);


                                                            int _ImportRetryCount = 5;
                                                            int _ImportRetryInitialSleep = 500;
                                                            int retryCount = 0;
                                                            while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                                            {
                                                                imageResult.Dispose();
                                                                Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                                                imageResult = await client.GetAsync(imageUrl);
                                                                retryCount++;
                                                            }

                                                            if (imageResult.IsSuccessStatusCode)
                                                            {
                                                                using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                                                {
                                                                    RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, imageUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                                    if (picture.Result == null)
                                                                    {
                                                                        throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                                                    }

                                                                    page.Images = new RPictureFile[] { picture.Result };
                                                                    page.CoverImageIndex = 0;

                                                                    if (book.CoverItemIndex == (order - 1))
                                                                    {
                                                                        book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                    }

                                                                    pages.Add(page);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                                {
                                                                    job.EndTime = DateTime.Now;
                                                                    job.Status = ImportJobStatus.Failed;
                                                                    job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                    importJobUpdaterDb.Update(job);
                                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                                }
                                                                
                                                                imageResult.Dispose();
                                                                return;
                                                            }

                                                            imageResult.Dispose();
                                                            GC.Collect();
                                                        }


                                                        



                                                    }
                                                }

                                                book.Items = pages.ToArray();
                                                book.ItemCount = pages.Count;

                                                if (book.CoverImage == null && pages.Count > 0)
                                                {
                                                    book.CoverImage = RPictureFile.Duplicate(pages[0].Images.First());
                                                }

                                                await context.Artifacts.AddAsync(book);
                                                await context.SaveChangesAsync();

                                                job.ProgressPercent = 100;
                                                job.Status = ImportJobStatus.Succeeded;
                                                job.ArtifactId = book.Id;
                                                job.EndTime = DateTime.Now;
                                                context.Update(job);
                                                await context.SaveChangesAsync();

                                            }

                                        }
                                        else
                                        {
                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                            {
                                                job.EndTime = DateTime.Now;
                                                job.Status = ImportJobStatus.Failed;
                                                job.Exception = $"Http result is not ok ({result.StatusCode}) for {url}";
                                                importJobUpdaterDb.Update(job);
                                                await importJobUpdaterDb.SaveChangesAsync();
                                            }
                                            return;
                                        }
                                    }

                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }                               

                            }
                        }
                    );


                
                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// from http://pudl.princeton.edu/
        /// </summary>
        /// <param name="resourceNumber">dj52w476m</param>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromPrinceton(string resourceNumber, string friendlyUrl)
        {
            string url = $"http://pudl.princeton.edu/mdCompiler2.php?obj={resourceNumber}";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.Princeton && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                    .SingleOrDefaultAsync()
                )
                !=
                null
                )
            {
                return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing {url}");
            }

            if (string.IsNullOrEmpty(friendlyUrl))
            {
                friendlyUrl = resourceNumber;
            }

            if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
               )
            {
                return new RServiceResult<bool>(false, $"duplicated friendly url '{friendlyUrl}'");
            }

            ImportJob job = new ImportJob()
            {
                JobType = JobType.Princeton,
                ResourceNumber = resourceNumber,
                FriendlyUrl = friendlyUrl,
                SrcUrl = url,
                QueueTime = DateTime.Now,
                ProgressPercent = 0,
                Status = ImportJobStatus.NotStarted
            };


            await _context.ImportJobs.AddAsync
                (
                job
                );

            await _context.SaveChangesAsync();


            try
            {

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                        try
                        {
                            

                            using (var client = new HttpClient())
                            {

                                using (var result = await client.GetAsync(url))
                                {
                                        if (result.IsSuccessStatusCode)
                                        {
                                            using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                            {
                                                RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from {url}", $"extracted from {url}")
                                                {
                                                    Status = PublishStatus.Draft,
                                                    DateTime = DateTime.Now,
                                                    LastModified = DateTime.Now,
                                                    CoverItemIndex = 0,
                                                    FriendlyUrl = friendlyUrl
                                                };


                                                List<RTagValue> meta = new List<RTagValue>();
                                                RTagValue tag;

                                                string xml = await result.Content.ReadAsStringAsync();


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = xml;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }

                                                XElement elObject = XDocument.Parse(xml).Root;
                                                foreach(var prop in  elObject.Element("dmd").Element("properties").Elements("property"))
                                                {
                                                    if (prop.Element("label") == null)
                                                        continue;
                                                    string label = prop.Element("label").Value.Replace(":", "");
                                                    int order = 1;
                                                    foreach (var value in prop.Elements("valueGrp").Elements("value"))
                                                    {
                                                        tag = await TagHandler.PrepareAttribute(context, label, value.Value, order);
                                                        if(value.Attribute("href") != null)
                                                        {
                                                            if(value.Attribute("href").Value.IndexOf("http://localhost") != 0)
                                                            {
                                                                tag.ValueSupplement = value.Attribute("href").Value;
                                                            }
                                                        }
                                                        meta.Add(tag);

                                                        if(label == "Title")
                                                        {
                                                            book.Name = book.NameInEnglish = 
                                                            book.Description = book.DescriptionInEnglish = 
                                                            value.Value;
                                                        }
                                                        order++;
                                                    }                                                      
                                                }

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "Princeton Digital Library of Islamic Manuscripts", 1);
                                                tag.ValueSupplement = $"http://pudl.princeton.edu/objects/{job.ResourceNumber}";                                                

                                                meta.Add(tag);


                                                book.Tags = meta.ToArray();
                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                foreach (var structure in elObject.Elements("structure"))
                                                {
                                                    if (structure.Attribute("type") != null && structure.Attribute("type").Value == "RelatedObjects")
                                                    {                                                        
                                                        if (structure.Element("div") == null || structure.Element("div").Element("OrderedList") == null)
                                                        {
                                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                            {
                                                                job.EndTime = DateTime.Now;
                                                                job.Status = ImportJobStatus.Failed;
                                                                job.Exception = "structure[RelatedObjects].div.OrderedList is null";
                                                                importJobUpdaterDb.Update(job);
                                                                await importJobUpdaterDb.SaveChangesAsync();
                                                                return;
                                                            }
                                                        }


                                                        int pageCount = structure.Element("div").Element("OrderedList").Elements("div").Count();
                                                        int inlineOrder = 0;
                                                        
                                                        foreach (var div in structure.Element("div").Element("OrderedList").Elements("div"))
                                                        {
                                                            inlineOrder++;
                                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                            {
                                                                job.ProgressPercent = inlineOrder * 100 / (decimal)pageCount;
                                                                importJobUpdaterDb.Update(job);
                                                                await importJobUpdaterDb.SaveChangesAsync();
                                                            }

                                                            int order = int.Parse(div.Attribute("order").Value);
                                                            RArtifactItemRecord page = new RArtifactItemRecord()
                                                            {
                                                                Name = $"تصویر {order}",
                                                                NameInEnglish = div.Attribute("label").Value,
                                                                Description = "",
                                                                DescriptionInEnglish = "",
                                                                Order = order,
                                                                FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                                                LastModified = DateTime.Now
                                                            };

                                                            string imageUrl = div.Attribute("img").Value;
                                                            imageUrl = "https://libimages.princeton.edu/loris/" + imageUrl.Substring(imageUrl.LastIndexOf(":") + 1);
                                                            imageUrl += $"/full/,{div.Attribute("h").Value}/0/default.jpg";

                                                            tag = await TagHandler.PrepareAttribute(context, "Source", "Princeton Digital Library of Islamic Manuscripts", 1);
                                                            tag.ValueSupplement = imageUrl;
                                                            page.Tags = new RTagValue[] { tag };

                                                            if (!string.IsNullOrEmpty(imageUrl))
                                                            {
                                                                bool recovered = false;
                                                                if (
                                                                   File.Exists
                                                                   (
                                                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                   )
                                                                   &&
                                                                   File.Exists
                                                                   (
                                                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                   )
                                                                   &&
                                                                   File.Exists
                                                                   (
                                                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                   )
                                                                   )
                                                                {
                                                                    RServiceResult<RPictureFile> picture = await _pictureFileService.RecoverFromeFiles(page.Name, page.Description, 1,
                                                                        imageUrl,
                                                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                        $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                                    if (picture.Result != null)
                                                                    {
                                                                        recovered = true;
                                                                        page.Images = new RPictureFile[] { picture.Result };
                                                                        page.CoverImageIndex = 0;

                                                                        if (book.CoverItemIndex == (order - 1))
                                                                        {
                                                                            book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                        }

                                                                        pages.Add(page);
                                                                    }

                                                                }
                                                                if (!recovered)
                                                                {
                                                                    if (
                                                                       File.Exists
                                                                       (
                                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                       )

                                                                   )
                                                                    {
                                                                        File.Delete
                                                                       (
                                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                       );
                                                                    }
                                                                    if (

                                                                       File.Exists
                                                                       (
                                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                       )

                                                                   )
                                                                    {
                                                                        File.Delete
                                                                        (
                                                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                        );
                                                                    }
                                                                    if (

                                                                       File.Exists
                                                                       (
                                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                       )
                                                                   )
                                                                    {
                                                                        File.Delete
                                                                        (
                                                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                        );
                                                                    }
                                                                    var imageResult = await client.GetAsync(imageUrl);


                                                                    int _ImportRetryCount = 5;
                                                                    int _ImportRetryInitialSleep = 500;
                                                                    int retryCount = 0;
                                                                    while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                                                    {
                                                                        imageResult.Dispose();
                                                                        Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                                                        imageResult = await client.GetAsync(imageUrl);
                                                                        retryCount++;
                                                                    }

                                                                    if (imageResult.IsSuccessStatusCode)
                                                                    {
                                                                        using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                                                        {
                                                                            RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, imageUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                                            if (picture.Result == null)
                                                                            {
                                                                                throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                                                            }

                                                                            page.Images = new RPictureFile[] { picture.Result };
                                                                            page.CoverImageIndex = 0;

                                                                            if (book.CoverItemIndex == (order - 1))
                                                                            {
                                                                                book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                            }

                                                                            pages.Add(page);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                                        {
                                                                            job.EndTime = DateTime.Now;
                                                                            job.Status = ImportJobStatus.Failed;
                                                                            job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                            importJobUpdaterDb.Update(job);
                                                                            await importJobUpdaterDb.SaveChangesAsync();
                                                                        }

                                                                        imageResult.Dispose();
                                                                        return;
                                                                    }

                                                                    imageResult.Dispose();
                                                                    GC.Collect();
                                                                }

                                                            }



                                                        }
                                                                                                                
                                                    }
                                                }

                                                foreach (var structure in elObject.Elements("structure"))
                                                {
                                                    if (structure.Attribute("type") != null && structure.Attribute("type").Value == "Physical")
                                                    {
                                                        if(structure.Element("RTLBoundManuscript") != null)
                                                        {
                                                            foreach (var leaf in structure.Element("RTLBoundManuscript").Elements("Leaf"))
                                                            {
                                                                foreach (var side in leaf.Elements("Side"))
                                                                {
                                                                    int pageOrder = int.Parse(side.Attribute("order").Value);
                                                                    tag = await TagHandler.PrepareAttribute(context, "Leaf Side", side.Attribute("label").Value, 100);
                                                                    RArtifactItemRecord page = pages.Where(p => p.Order == pageOrder).SingleOrDefault();
                                                                    if (page != null)
                                                                    {
                                                                        List<RTagValue> tags = new List<RTagValue>(page.Tags);
                                                                        tags.Add(tag);
                                                                        page.Tags = tags;
                                                                    }
                                                                }
                                                            }
                                                            foreach (var folio in structure.Element("RTLBoundManuscript").Elements("Folio"))
                                                            {
                                                                foreach (var side in folio.Elements("Side"))
                                                                {
                                                                    int pageOrder = int.Parse(side.Attribute("order").Value);
                                                                    tag = await TagHandler.PrepareAttribute(context, "Folio Side", folio.Attribute("label").Value + ":" + side.Attribute("label").Value, 101);
                                                                    RArtifactItemRecord page = pages.Where(p => p.Order == pageOrder).SingleOrDefault();
                                                                    if (page != null)
                                                                    {
                                                                        List<RTagValue> tags = new List<RTagValue>(page.Tags);
                                                                        tags.Add(tag);
                                                                        page.Tags = tags;
                                                                    }
                                                                }
                                                            }
                                                        }                                                        
                                                    }
                                                }

                                                book.Items = pages.ToArray();
                                                book.ItemCount = pages.Count;

                                                if(pages.Count == 0)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "ages.Count == 0";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }                                             

                                                await context.Artifacts.AddAsync(book);
                                                await context.SaveChangesAsync();

                                                job.ProgressPercent = 100;
                                                job.Status = ImportJobStatus.Succeeded;
                                                job.ArtifactId = book.Id;
                                                job.EndTime = DateTime.Now;
                                                context.Update(job);
                                                await context.SaveChangesAsync();
                                                


                                            }
                                        }
                                        else
                                        {
                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                            {
                                                job.EndTime = DateTime.Now;
                                                job.Status = ImportJobStatus.Failed;
                                                job.Exception = $"Http result is not ok ({result.StatusCode}) for {url}";
                                                importJobUpdaterDb.Update(job);
                                                await importJobUpdaterDb.SaveChangesAsync();
                                            }
                                            return;
                                        }

                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );



                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// from https://curiosity.lib.harvard.edu
        /// </summary>
        /// <param name="url">example: https://curiosity.lib.harvard.edu/islamic-heritage-project/catalog/40-990114893240203941</param>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromHarvard(string url, string friendlyUrl)
        {
            try
            {
                if (
                    (
                    await _context.ImportJobs
                        .Where(j => j.JobType == JobType.Harvard && j.ResourceNumber == url && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                        .SingleOrDefaultAsync()
                    )
                    !=
                    null
                    )
                {
                    return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing {url}");
                }

                if (string.IsNullOrEmpty(friendlyUrl))
                {
                    return new RServiceResult<bool>(false, $"Friendly url is empty, url = {url}");
                }

                if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
                )
                {
                    return new RServiceResult<bool>(false, $"duplicated friendly url '{friendlyUrl}'");
                }

                ImportJob job = new ImportJob()
                {
                    JobType = JobType.Harvard,
                    ResourceNumber = url,
                    FriendlyUrl = friendlyUrl,
                    SrcUrl = url,
                    QueueTime = DateTime.Now,
                    ProgressPercent = 0,
                    Status = ImportJobStatus.NotStarted
                };


                await _context.ImportJobs.AddAsync
                    (
                    job
                    );

                await _context.SaveChangesAsync();

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {
                                using (var client = new HttpClient())
                                {

                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {
                                            using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                            {
                                                RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from {url}", $"extracted from {url}")
                                                {
                                                    Status = PublishStatus.Draft,
                                                    DateTime = DateTime.Now,
                                                    LastModified = DateTime.Now,
                                                    CoverItemIndex = 0,
                                                    FriendlyUrl = friendlyUrl
                                                };


                                                List<RTagValue> meta = new List<RTagValue>();
                                                RTagValue tag;

                                                string html = await result.Content.ReadAsStringAsync();


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = html;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }

                                                int nStartIndex = html.IndexOf("<dt");
                                                while(nStartIndex != -1)
                                                {
                                                    nStartIndex = html.IndexOf(">", nStartIndex);
                                                    if (nStartIndex == -1)
                                                        break;
                                                    nStartIndex++;
                                                    string tagName = html.Substring(nStartIndex, html.IndexOf(":", nStartIndex) - nStartIndex);
                                                    nStartIndex = html.IndexOf("<dd", nStartIndex);
                                                    if (nStartIndex == -1)
                                                        break;
                                                    nStartIndex = html.IndexOf(">", nStartIndex);
                                                    if (nStartIndex == -1)
                                                        break;
                                                    nStartIndex++;
                                                    string tagValues = html.Substring(nStartIndex, html.IndexOf("</dd>", nStartIndex) - nStartIndex);
                                                    foreach(string tagValuePart in tagValues.Split("<br/>", StringSplitOptions.RemoveEmptyEntries))
                                                    {
                                                        string tagValue = tagValuePart;
                                                        bool href = false;
                                                        if(tagValue.IndexOf("<a href=") != -1)
                                                        {
                                                            href = true;
                                                            tagValue = tagValue.Substring(tagValue.IndexOf('>') + 1);
                                                            tagValue = tagValue.Substring(0, tagValue.IndexOf('<'));
                                                        }
                                                        tag = await TagHandler.PrepareAttribute(context, tagName, tagValue, 1);
                                                        if (href)
                                                            tag.ValueSupplement = tagValue;
                                                        meta.Add(tag);
                                                    }

                                                    nStartIndex = html.IndexOf("<dt", nStartIndex + 1);
                                                }                                              




                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "Harvard University Islamic Heritage Project", 1);
                                                tag.ValueSupplement = $"{job.SrcUrl}";

                                                meta.Add(tag);

                                                nStartIndex = html.IndexOf("https://pds.lib.harvard.edu/pds/view/");
                                                if(nStartIndex == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "Not found https://pds.lib.harvard.edu/pds/view/";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nStartIndex += "https://pds.lib.harvard.edu/pds/view/".Length;

                                                string hardvardResourceNumber = html.Substring(nStartIndex, html.IndexOf('\"', nStartIndex) - nStartIndex);

                                                List<RArtifactItemRecord> pages = (await _InternalHarvardJsonImport(hardvardResourceNumber, job, friendlyUrl, context, book, meta)).Result;
                                                if(pages == null)
                                                {
                                                    return;
                                                }


                                                book.Tags = meta.ToArray();                                                                                         

                                                book.Items = pages.ToArray();
                                                book.ItemCount = pages.Count;

                                                if (pages.Count == 0)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "Pages.Count == 0";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                await context.Artifacts.AddAsync(book);
                                                await context.SaveChangesAsync();

                                                job.ProgressPercent = 100;
                                                job.Status = ImportJobStatus.Succeeded;
                                                job.ArtifactId = book.Id;
                                                job.EndTime = DateTime.Now;
                                                context.Update(job);
                                                await context.SaveChangesAsync();



                                            }
                                        }
                                        else
                                        {
                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                            {
                                                job.EndTime = DateTime.Now;
                                                job.Status = ImportJobStatus.Failed;
                                                job.Exception = $"Http result is not ok ({result.StatusCode}) for {url}";
                                                importJobUpdaterDb.Update(job);
                                                await importJobUpdaterDb.SaveChangesAsync();
                                            }
                                            return;
                                        }

                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private async Task<RServiceResult<List<RArtifactItemRecord>>> _InternalHarvardJsonImport(string hardvardResourceNumber, ImportJob job, string friendlyUrl, RMuseumDbContext context, RArtifactMasterRecord book, List<RTagValue> meta)
        {
            List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

            using (var client = new HttpClient())
            {
                using (var jsonResult = await client.GetAsync($"https://iiif.lib.harvard.edu/manifests/drs:{hardvardResourceNumber}"))
                {
                    if (jsonResult.IsSuccessStatusCode)
                    {
                        string json = await jsonResult.Content.ReadAsStringAsync();
                        var parsed = JObject.Parse(json);
                        book.Name = book.NameInEnglish = book.Description = book.DescriptionInEnglish =
                            parsed.SelectToken("label").Value<string>();

                       
                        RTagValue tag;

                        tag = await TagHandler.PrepareAttribute(context, "Title", book.Name, 1);
                        meta.Add(tag);

                        tag = await TagHandler.PrepareAttribute(context, "Contributor Names", "تعیین نشده", 1);
                        meta.Add(tag);

                        List<string> labels = new List<string>();
                        foreach (JToken structure in parsed.SelectTokens("$.structures[*].label"))
                        {
                            labels.Add(structure.Value<string>());
                        }

                        int order = 0;
                        var canvases = parsed.SelectToken("sequences").First().SelectToken("canvases").ToArray();
                        int pageCount = canvases.Length;
                        foreach (JToken canvas in canvases)
                        {
                            order++;
                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                            {
                                job.ProgressPercent = order * 100 / (decimal)pageCount;
                                importJobUpdaterDb.Update(job);
                                await importJobUpdaterDb.SaveChangesAsync();
                            }
                            string label = canvas.SelectToken("label").Value<string>();
                            if (labels.Where(l => l.IndexOf(label) != -1).SingleOrDefault() != null)
                                label = labels.Where(l => l.IndexOf(label) != -1).SingleOrDefault();
                            string imageUrl = canvas.SelectTokens("images[*]").First().SelectToken("resource").SelectToken("@id").Value<string>();
                            RArtifactItemRecord page = new RArtifactItemRecord()
                            {
                                Name = $"تصویر {order}",
                                NameInEnglish = label,
                                Description = "",
                                DescriptionInEnglish = "",
                                Order = order,
                                FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                LastModified = DateTime.Now
                            };



                            tag = await TagHandler.PrepareAttribute(context, "Source", "Harvard University Islamic Heritage Project", 1);
                            tag.ValueSupplement = imageUrl;
                            page.Tags = new RTagValue[] { tag };


                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                bool recovered = false;
                                if (
                                   File.Exists
                                   (
                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                   )
                                   &&
                                   File.Exists
                                   (
                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                   )
                                   &&
                                   File.Exists
                                   (
                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                   )
                                   )
                                {
                                    RServiceResult<RPictureFile> picture = await _pictureFileService.RecoverFromeFiles(page.Name, page.Description, 1,
                                        imageUrl,
                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                        $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                    if (picture.Result != null)
                                    {
                                        recovered = true;
                                        page.Images = new RPictureFile[] { picture.Result };
                                        page.CoverImageIndex = 0;

                                        if (book.CoverItemIndex == (order - 1))
                                        {
                                            book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                        }

                                        pages.Add(page);
                                    }

                                }
                                if (!recovered)
                                {
                                    if (
                                       File.Exists
                                       (
                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                       )

                                   )
                                    {
                                        File.Delete
                                       (
                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                       );
                                    }
                                    if (

                                       File.Exists
                                       (
                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                       )

                                   )
                                    {
                                        File.Delete
                                        (
                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                        );
                                    }
                                    if (

                                       File.Exists
                                       (
                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                       )
                                   )
                                    {
                                        File.Delete
                                        (
                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                        );
                                    }
                                    var imageResult = await client.GetAsync(imageUrl);


                                    int _ImportRetryCount = 5;
                                    int _ImportRetryInitialSleep = 500;
                                    int retryCount = 0;
                                    while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                    {
                                        imageResult.Dispose();
                                        Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                        imageResult = await client.GetAsync(imageUrl);
                                        retryCount++;
                                    }

                                    if (imageResult.IsSuccessStatusCode)
                                    {
                                        using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                        {
                                            RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, imageUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                            if (picture.Result == null)
                                            {
                                                throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                            }

                                            page.Images = new RPictureFile[] { picture.Result };
                                            page.CoverImageIndex = 0;

                                            if (book.CoverItemIndex == (order - 1))
                                            {
                                                book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                            }

                                            pages.Add(page);
                                        }
                                    }
                                    else
                                    {
                                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                        {
                                            job.EndTime = DateTime.Now;
                                            job.Status = ImportJobStatus.Failed;
                                            job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                            importJobUpdaterDb.Update(job);
                                            await importJobUpdaterDb.SaveChangesAsync();
                                        }

                                        imageResult.Dispose();
                                        return new RServiceResult<List<RArtifactItemRecord>>(null, "failed");
                                    }

                                    imageResult.Dispose();
                                    GC.Collect();
                                }
                            }
                        }
                    }
                    else
                    {
                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                        {
                            job.EndTime = DateTime.Now;
                            job.Status = ImportJobStatus.Failed;
                            job.Exception = $"Http result is not ok ({jsonResult.StatusCode}) for https://iiif.lib.harvard.edu/manifests/drs:{hardvardResourceNumber}";
                            importJobUpdaterDb.Update(job);
                            await importJobUpdaterDb.SaveChangesAsync();
                        }
                        return new RServiceResult<List<RArtifactItemRecord>>(null, "failed");
                    }
                }
            }

            return new RServiceResult<List<RArtifactItemRecord>>(pages);

            
        }

        /// <summary>
        /// import from http://www.qajarwomen.org
        /// </summary>
        /// <param name="hardvardResourceNumber">43117279</param>
        /// <param name="friendlyUrl">atame</param>
        /// <param name="srcUrl">http://www.qajarwomen.org/fa/items/1018A10.html</param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromHarvardDirectly(string hardvardResourceNumber, string friendlyUrl, string srcUrl)
        {
            try
            {
                if (
                    (
                    await _context.ImportJobs
                        .Where(j => j.JobType == JobType.HarvardDirect && j.ResourceNumber == hardvardResourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                        .SingleOrDefaultAsync()
                    )
                    !=
                    null
                    )
                {
                    return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing harvard direct resource number {hardvardResourceNumber}");
                }

                if (string.IsNullOrEmpty(friendlyUrl))
                {
                    return new RServiceResult<bool>(false, $"Friendly url is empty, harvard direct resource number {hardvardResourceNumber}");
                }

                if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
                )
                {
                    return new RServiceResult<bool>(false, $"duplicated friendly url '{friendlyUrl}'");
                }

                ImportJob job = new ImportJob()
                {
                    JobType = JobType.HarvardDirect,
                    ResourceNumber = hardvardResourceNumber,
                    FriendlyUrl = friendlyUrl,
                    SrcUrl = srcUrl,
                    QueueTime = DateTime.Now,
                    ProgressPercent = 0,
                    Status = ImportJobStatus.NotStarted
                };


                await _context.ImportJobs.AddAsync
                    (
                    job
                    );

                await _context.SaveChangesAsync();

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {
                                using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                {
                                    RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from harvard resource number {job.ResourceNumber}", $"extracted from harvard resource number {job.ResourceNumber}")
                                    {
                                        Status = PublishStatus.Draft,
                                        DateTime = DateTime.Now,
                                        LastModified = DateTime.Now,
                                        CoverItemIndex = 0,
                                        FriendlyUrl = friendlyUrl
                                    };


                                    List<RTagValue> meta = new List<RTagValue>();
                                    RTagValue tag;





                                    tag = await TagHandler.PrepareAttribute(context, "Notes", "وارد شده از سایت دنیای زنان در عصر قاجار", 1);
                                    meta.Add(tag);



                                    tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                    meta.Add(tag);

                                    tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                    meta.Add(tag);


                                    tag = await TagHandler.PrepareAttribute(context, "Source", "دنیای زنان در عصر قاجار", 1);
                                    tag.ValueSupplement = $"{job.SrcUrl}";

                                    meta.Add(tag);

                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                    {
                                        job.StartTime = DateTime.Now;
                                        job.Status = ImportJobStatus.Running;
                                        job.SrcContent = $"https://iiif.lib.harvard.edu/manifests/drs:{hardvardResourceNumber}";
                                        importJobUpdaterDb.Update(job);
                                        await importJobUpdaterDb.SaveChangesAsync();
                                    }

                                    List<RArtifactItemRecord> pages = (await _InternalHarvardJsonImport(hardvardResourceNumber, job, friendlyUrl, context, book, meta)).Result;
                                    if (pages == null)
                                    {
                                        return;
                                    }


                                    book.Tags = meta.ToArray();

                                    book.Items = pages.ToArray();
                                    book.ItemCount = pages.Count;

                                    if (pages.Count == 0)
                                    {
                                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                        {
                                            job.EndTime = DateTime.Now;
                                            job.Status = ImportJobStatus.Failed;
                                            job.Exception = "Pages.Count == 0";
                                            importJobUpdaterDb.Update(job);
                                            await importJobUpdaterDb.SaveChangesAsync();
                                        }
                                        return;
                                    }

                                    await context.Artifacts.AddAsync(book);
                                    await context.SaveChangesAsync();

                                    job.ProgressPercent = 100;
                                    job.Status = ImportJobStatus.Succeeded;
                                    job.ArtifactId = book.Id;
                                    job.EndTime = DateTime.Now;
                                    context.Update(job);
                                    await context.SaveChangesAsync();



                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );

                return new RServiceResult<bool>(true);
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
        

        /// <summary>
        /// from https://catalog.hathitrust.org
        /// </summary>
        /// <param name="resourceNumber">006814127</param>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromHathiTrust(string resourceNumber, string friendlyUrl)
        {
            string url = $"https://catalog.hathitrust.org/Record/{resourceNumber}.xml";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.HathiTrust && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                    .SingleOrDefaultAsync()
                )
                !=
                null
                )
            {
                return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing {url}");
            }

            if (string.IsNullOrEmpty(friendlyUrl))
            {
                friendlyUrl = resourceNumber;
            }

            if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
               )
            {
                return new RServiceResult<bool>(false, $"duplicated artifact friendly url '{friendlyUrl}'");
            }

           

            ImportJob job = new ImportJob()
            {
                JobType = JobType.HathiTrust,
                ResourceNumber = resourceNumber,
                FriendlyUrl = friendlyUrl,
                SrcUrl = url,
                QueueTime = DateTime.Now,
                ProgressPercent = 0,
                Status = ImportJobStatus.NotStarted
            };


            await _context.ImportJobs.AddAsync
                (
                job
                );

            await _context.SaveChangesAsync();


            try
            {

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {


                                using (var client = new HttpClient())
                                {

                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {
                                            using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                            {
                                                if (
                                                   (await context.Artifacts.Where(a => a.FriendlyUrl == job.FriendlyUrl).SingleOrDefaultAsync())
                                                   !=
                                                   null
                                                  )
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "aborted because of duplicated friendly url";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }
                                                RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from {url}", $"extracted from {url}")
                                                {
                                                    Status = PublishStatus.Draft,
                                                    DateTime = DateTime.Now,
                                                    LastModified = DateTime.Now,
                                                    CoverItemIndex = 0,
                                                    FriendlyUrl = friendlyUrl
                                                };


                                                List<RTagValue> meta = new List<RTagValue>();
                                                RTagValue tag;

                                                string xml = await result.Content.ReadAsStringAsync();


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = xml;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }

                                                string title = "";
                                                string author = "";
                                                string pdfResourceNumber = "";
                                                int tagOrder = 1;
                                                XElement elObject = XDocument.Parse(xml).Root;
                                                foreach (var datafield in elObject.Element("record").Elements("datafield"))
                                                {
                                                    tagOrder++;
                                                    if (datafield.Attribute("tag") == null)
                                                        continue;
                                                    string hathiTrustTag = datafield.Attribute("tag").Value;
                                                    switch(hathiTrustTag)
                                                    {
                                                        case "245":
                                                        case "246":
                                                            foreach (var subfield in datafield.Elements("subfield"))
                                                            {
                                                                if(subfield.Attribute("code") != null)
                                                                {
                                                                    if (subfield.Attribute("code").Value == "a" || subfield.Attribute("code").Value == "f")
                                                                        title = (title + " " + subfield.Value).Trim();
                                                                }
                                                            }
                                                            break;
                                                        case "100":
                                                            foreach (var subfield in datafield.Elements("subfield"))
                                                            {
                                                                if (subfield.Attribute("code") != null)
                                                                {
                                                                    if (subfield.Attribute("code").Value == "a" || subfield.Attribute("code").Value == "d")
                                                                        author = (author + " " + subfield.Value).Trim();
                                                                }
                                                            }
                                                            break;
                                                        case "HOL":
                                                            foreach (var subfield in datafield.Elements("subfield"))
                                                            {
                                                                if (subfield.Attribute("code") != null)
                                                                {
                                                                    if (subfield.Attribute("code").Value == "p")
                                                                        pdfResourceNumber = subfield.Value;
                                                                }
                                                            }
                                                            break;

                                                        default:
                                                            {
                                                                if(int.TryParse(hathiTrustTag, out int tmp))
                                                                {
                                                                    if(tmp >= 100 && tmp <= 900)
                                                                    {
                                                                        string note = "";
                                                                        foreach (var subfield in datafield.Elements("subfield"))
                                                                        {
                                                                            if (subfield.Attribute("code") != null)
                                                                            {                                                                                
                                                                                    note = (note + " " + subfield.Value).Trim();
                                                                            }
                                                                        }

                                                                        tag = await TagHandler.PrepareAttribute(context, "Notes", note, tagOrder);
                                                                        meta.Add(tag);
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                    }                                                        
                                                    
                                                }

                                                if(string.IsNullOrEmpty(pdfResourceNumber))
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "pdfResourceNumber not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                tag = await TagHandler.PrepareAttribute(context, "Title", title, 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Contributor Names", author, 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "HathiTrust Digital Library", 1);
                                                string viewerUrl = $"https://babel.hathitrust.org/cgi/pt?id={pdfResourceNumber}";
                                                tag.ValueSupplement = viewerUrl;

                                                meta.Add(tag);

                                                book.Name = book.NameInEnglish = book.Description = book.DescriptionInEnglish = title;
                                                book.Tags = meta.ToArray();

                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                string lastMD5hash = "";
                                                int order = 0;
                                                while(true)
                                                {
                                                    order++;
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.ProgressPercent = order;
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }

                                                    string imageUrl = $"https://babel.hathitrust.org/cgi/imgsrv/image?id={pdfResourceNumber};seq={order};size=1000;rotation=0";
                                                    RArtifactItemRecord page = new RArtifactItemRecord()
                                                    {
                                                        Name = $"تصویر {order}",
                                                        NameInEnglish = $"Image {order}",
                                                        Description = "",
                                                        DescriptionInEnglish = "",
                                                        Order = order,
                                                        FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                                        LastModified = DateTime.Now
                                                    };

                                                    tag = await TagHandler.PrepareAttribute(context, "Source", "HathiTrust Digital Library", 1);
                                                    tag.ValueSupplement = viewerUrl;
                                                    page.Tags = new RTagValue[] { tag };

                                                    if (!string.IsNullOrEmpty(imageUrl))
                                                    {
                                                        bool recovered = false;
                                                        if (
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           &&
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           &&
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           )
                                                        {
                                                            RServiceResult<RPictureFile> picture = await _pictureFileService.RecoverFromeFiles(page.Name, page.Description, 1,
                                                                imageUrl,
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                            if (picture.Result != null)
                                                            {
                                                                recovered = true;
                                                                page.Images = new RPictureFile[] { picture.Result };
                                                                page.CoverImageIndex = 0;

                                                                if (book.CoverItemIndex == (order - 1))
                                                                {
                                                                    book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                }

                                                                pages.Add(page);
                                                            }

                                                        }
                                                        if (!recovered)
                                                        {
                                                            if (
                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )

                                                           )
                                                            {
                                                                File.Delete
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               );
                                                            }
                                                            if (

                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )

                                                           )
                                                            {
                                                                File.Delete
                                                                (
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                );
                                                            }
                                                            if (

                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )
                                                           )
                                                            {
                                                                File.Delete
                                                                (
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                );
                                                            }
                                                            var imageResult = await client.GetAsync(imageUrl);


                                                            int _ImportRetryCount = 200;
                                                            int _ImportRetryInitialSleep = 500;
                                                            int retryCount = 0;
                                                            while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                                            {
                                                                imageResult.Dispose();
                                                                Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                                                imageResult = await client.GetAsync(imageUrl);
                                                                retryCount++;
                                                            }

                                                            if (imageResult.IsSuccessStatusCode)
                                                            {
                                                                using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                                                {
                                                                    RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, imageUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                                    if (picture.Result == null)
                                                                    {
                                                                        throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                                                    }

                                                                    bool lastPage = false;
                                                                    using (var md5 = MD5.Create())
                                                                    {
                                                                        string md5hash = string.Join("", md5.ComputeHash(File.ReadAllBytes(Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"))).Select(x => x.ToString("X2")));
                                                                        if (md5hash == lastMD5hash)
                                                                        {
                                                                            File.Delete
                                                                                (
                                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                                );
                                                                            File.Delete
                                                                                (
                                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                                );
                                                                            File.Delete
                                                                                (
                                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                                );
                                                                            lastPage = true;
                                                                        }
                                                                        lastMD5hash = md5hash;
                                                                    }

                                                                    if(!lastPage)
                                                                    {
                                                                        page.Images = new RPictureFile[] { picture.Result };
                                                                        page.CoverImageIndex = 0;

                                                                        if (book.CoverItemIndex == (order - 1))
                                                                        {
                                                                            book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                        }

                                                                        pages.Add(page);
                                                                    }
                                                                    else
                                                                    {
                                                                        break;
                                                                    }
                                                                    
                                                                }
                                                            }
                                                            else
                                                            {
                                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                                {
                                                                    job.EndTime = DateTime.Now;
                                                                    job.Status = ImportJobStatus.Failed;
                                                                    job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                    importJobUpdaterDb.Update(job);
                                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                                }

                                                                imageResult.Dispose();
                                                                return;
                                                            }

                                                            imageResult.Dispose();
                                                            GC.Collect();
                                                        }
                                                    }
                                                }
                                                
                                               
                                                book.Items = pages.ToArray();
                                                book.ItemCount = pages.Count;

                                                if (pages.Count == 0)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "ages.Count == 0";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                await context.Artifacts.AddAsync(book);
                                                await context.SaveChangesAsync();

                                                job.ProgressPercent = 100;
                                                job.Status = ImportJobStatus.Succeeded;
                                                job.ArtifactId = book.Id;
                                                job.EndTime = DateTime.Now;
                                                context.Update(job);
                                                await context.SaveChangesAsync();                                             


                                            }
                                        }
                                        else
                                        {
                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                            {
                                                job.EndTime = DateTime.Now;
                                                job.Status = ImportJobStatus.Failed;
                                                job.Exception = $"Http result is not ok ({result.StatusCode}) for {url}";
                                                importJobUpdaterDb.Update(job);
                                                await importJobUpdaterDb.SaveChangesAsync();
                                            }
                                            return;
                                        }

                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );



                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// from http://www.library.upenn.edu/
        /// </summary>
        /// <param name="resourceNumber">MEDREN_9949222153503681</param>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromPenLibraries(string resourceNumber, string friendlyUrl)
        {
            string url = $"http://dla.library.upenn.edu/dla/medren/pageturn.html?id={resourceNumber}&rotation=0&size=0";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.PennLibraries && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                    .SingleOrDefaultAsync()
                )
                !=
                null
                )
            {
                return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing {url}");
            }

            if (string.IsNullOrEmpty(friendlyUrl))
            {
                friendlyUrl = resourceNumber;
            }

            if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
               )
            {
                return new RServiceResult<bool>(false, $"duplicated artifact friendly url '{friendlyUrl}'");
            }



            ImportJob job = new ImportJob()
            {
                JobType = JobType.PennLibraries,
                ResourceNumber = resourceNumber,
                FriendlyUrl = friendlyUrl,
                SrcUrl = url,
                QueueTime = DateTime.Now,
                ProgressPercent = 0,
                Status = ImportJobStatus.NotStarted
            };


            await _context.ImportJobs.AddAsync
                (
                job
                );

            await _context.SaveChangesAsync();


            try
            {

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {


                                using (var client = new HttpClient())
                                {
                                    client.Timeout = TimeSpan.FromMinutes(5);
                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {
                                            using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                            {
                                                if (
                                                   (await context.Artifacts.Where(a => a.FriendlyUrl == job.FriendlyUrl).SingleOrDefaultAsync())
                                                   !=
                                                   null
                                                  )
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "aborted because of duplicated friendly url";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }
                                                RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from {url}", $"extracted from {url}")
                                                {
                                                    Status = PublishStatus.Draft,
                                                    DateTime = DateTime.Now,
                                                    LastModified = DateTime.Now,
                                                    CoverItemIndex = 0,
                                                    FriendlyUrl = friendlyUrl
                                                };


                                                List<RTagValue> meta = new List<RTagValue>();
                                                RTagValue tag;

                                                string html = await result.Content.ReadAsStringAsync();


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = html;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }

                                                string title = "";
                                                string author = "";
                                                int tagOrder = 1;

                                                int nIdxStart = html.IndexOf("https://repo.library.upenn.edu/djatoka/resolver?");
                                                if(nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "https://repo.library.upenn.edu/djatoka/resolver? not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                string firstImageUrl = html.Substring(nIdxStart, html.IndexOf('"', nIdxStart) - nIdxStart).Replace("&amp;", "&");

                                                nIdxStart = html.IndexOf("recordinfolabel");
                                                while(nIdxStart != -1)
                                                {
                                                    nIdxStart += "recordinfolabel\">".Length;
                                                    int nIdxEnd = html.IndexOf(":", nIdxStart);
                                                    string recordinfolabel = html.Substring(nIdxStart, nIdxEnd - nIdxStart);
                                                    nIdxStart = html.IndexOf("recordinfotext", nIdxEnd);
                                                    nIdxStart += "recordinfotext\">".Length;
                                                    nIdxEnd = html.IndexOf("</td>", nIdxStart);
                                                    string recordinfotext = html.Substring(nIdxStart, nIdxEnd - nIdxStart).Replace("</div>", "<div>").Replace("\n", "").Replace("\r", "").Trim();

                                                    string[] values = recordinfotext.Split("<div>", StringSplitOptions.RemoveEmptyEntries);

                                                    foreach(string value in values)
                                                    {
                                                        if (value.Trim().Length == 0)
                                                            continue;
                                                        if (recordinfolabel == "Title")
                                                        {
                                                            title = value.Trim();
                                                            tag = await TagHandler.PrepareAttribute(context, "Title", title, 1);
                                                            meta.Add(tag);
                                                        }
                                                        else
                                                        if (recordinfolabel == "Author")
                                                        {
                                                            author = value.Trim();
                                                            tag = await TagHandler.PrepareAttribute(context, "Contributor Names", author, 1);
                                                            meta.Add(tag);
                                                        }
                                                        else
                                                        {
                                                            tag = await TagHandler.PrepareAttribute(context, recordinfolabel, value.Trim(), tagOrder++);
                                                            meta.Add(tag);
                                                        }
                                                    }

                                                    nIdxStart = html.IndexOf("recordinfolabel", nIdxEnd);

                                                }                                               

                                               

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "Penn Libraries", 1);
                                                string viewerUrl = $"http://dla.library.upenn.edu/dla/medren/detail.html?id={resourceNumber}";
                                                tag.ValueSupplement = viewerUrl;

                                                meta.Add(tag);

                                                book.Name = book.NameInEnglish = book.Description = book.DescriptionInEnglish = title;
                                                book.Tags = meta.ToArray();

                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                int order = 0;
                                                while (true)
                                                {
                                                    order++;                                                   

                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.ProgressPercent = order;
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }

                                                    string imageUrl = firstImageUrl;
                                                    
                                                    RArtifactItemRecord page = new RArtifactItemRecord()
                                                    {
                                                        Name = $"تصویر {order}",
                                                        NameInEnglish = $"Image {order}",
                                                        Description = "",
                                                        DescriptionInEnglish = "",
                                                        Order = order,
                                                        FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                                        LastModified = DateTime.Now
                                                    };

                                                    tag = await TagHandler.PrepareAttribute(context, "Source", "Penn Libraries", 1);
                                                    tag.ValueSupplement = viewerUrl;
                                                    page.Tags = new RTagValue[] { tag };

                                                    if (!string.IsNullOrEmpty(imageUrl))
                                                    {
                                                        bool recovered = false;
                                                        if (
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           &&
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           &&
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           )
                                                        {
                                                            RServiceResult<RPictureFile> picture = await _pictureFileService.RecoverFromeFiles(page.Name, page.Description, 1,
                                                                imageUrl,
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                            if (picture.Result != null)
                                                            {
                                                                recovered = true;
                                                                page.Images = new RPictureFile[] { picture.Result };
                                                                page.CoverImageIndex = 0;

                                                                if (book.CoverItemIndex == (order - 1))
                                                                {
                                                                    book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                }

                                                                pages.Add(page);
                                                            }

                                                        }
                                                        if (!recovered)
                                                        {
                                                            if (
                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )

                                                           )
                                                            {
                                                                File.Delete
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               );
                                                            }
                                                            if (

                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )

                                                           )
                                                            {
                                                                File.Delete
                                                                (
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                );
                                                            }
                                                            if (

                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )
                                                           )
                                                            {
                                                                File.Delete
                                                                (
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                );
                                                            }

                                                            if (order > 1)
                                                            {
                                                                string pageUrl = $"http://dla.library.upenn.edu/dla/medren/pageturn.html?id={resourceNumber}&doubleside=0&rotation=0&size=0&currentpage={order}";
                                                                var pageResult = await client.GetAsync(pageUrl);

                                                                if (pageResult.StatusCode == HttpStatusCode.NotFound)
                                                                {
                                                                    break;//finished
                                                                }

                                                                string pageHtml = await pageResult.Content.ReadAsStringAsync();
                                                                nIdxStart = pageHtml.IndexOf("https://repo.library.upenn.edu/djatoka/resolver?");
                                                                if (nIdxStart == -1)
                                                                {
                                                                    if (order > 1)
                                                                        break; //finished
                                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                                    {
                                                                        job.EndTime = DateTime.Now;
                                                                        job.Status = ImportJobStatus.Failed;
                                                                        job.Exception = $"https://repo.library.upenn.edu/djatoka/resolver? not found on page {order}";
                                                                        importJobUpdaterDb.Update(job);
                                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                                    }
                                                                    return;
                                                                }

                                                                imageUrl = pageHtml.Substring(nIdxStart, pageHtml.IndexOf('"', nIdxStart) - nIdxStart).Replace("&amp;", "&");

                                                            }
                                                            var imageResult = await client.GetAsync(imageUrl);

                                                            if(imageResult.StatusCode == HttpStatusCode.NotFound)
                                                            {
                                                                break;//finished
                                                            }


                                                            int _ImportRetryCount = 200;
                                                            int _ImportRetryInitialSleep = 500;
                                                            int retryCount = 0;
                                                            while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                                            {
                                                                imageResult.Dispose();
                                                                Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                                                imageResult = await client.GetAsync(imageUrl);
                                                                retryCount++;
                                                            }

                                                            if (imageResult.IsSuccessStatusCode)
                                                            {
                                                                using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                                                {
                                                                    RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, imageUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                                    if (picture.Result == null)
                                                                    {
                                                                        throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                                                    }


                                                                    page.Images = new RPictureFile[] { picture.Result };
                                                                    page.CoverImageIndex = 0;

                                                                    if (book.CoverItemIndex == (order - 1))
                                                                    {
                                                                        book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                    }

                                                                    pages.Add(page);

                                                                }
                                                            }
                                                            else
                                                            {
                                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                                {
                                                                    job.EndTime = DateTime.Now;
                                                                    job.Status = ImportJobStatus.Failed;
                                                                    job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                    importJobUpdaterDb.Update(job);
                                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                                }

                                                                imageResult.Dispose();
                                                                return;
                                                            }

                                                            imageResult.Dispose();
                                                            GC.Collect();
                                                        }
                                                    }
                                                }


                                                book.Items = pages.ToArray();
                                                book.ItemCount = pages.Count;

                                                if (pages.Count == 0)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "Pages.Count == 0";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                await context.Artifacts.AddAsync(book);
                                                await context.SaveChangesAsync();

                                                job.ProgressPercent = 100;
                                                job.Status = ImportJobStatus.Succeeded;
                                                job.ArtifactId = book.Id;
                                                job.EndTime = DateTime.Now;
                                                context.Update(job);
                                                await context.SaveChangesAsync();


                                            }
                                        }
                                        else
                                        {
                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                            {
                                                job.EndTime = DateTime.Now;
                                                job.Status = ImportJobStatus.Failed;
                                                job.Exception = $"Http result is not ok ({result.StatusCode}) for {url}";
                                                importJobUpdaterDb.Update(job);
                                                await importJobUpdaterDb.SaveChangesAsync();
                                            }
                                            return;
                                        }

                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );



                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// from http://cudl.lib.cam.ac.uk
        /// </summary>
        /// <param name="resourceNumber">MS-RAS-00258</param>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromCambridge(string resourceNumber, string friendlyUrl)
        {
            string url = $"http://cudl.lib.cam.ac.uk/view/{resourceNumber}.json";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.Cambridge && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                    .SingleOrDefaultAsync()
                )
                !=
                null
                )
            {
                return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing {url}");
            }

            if (string.IsNullOrEmpty(friendlyUrl))
            {
                friendlyUrl = resourceNumber;
            }

            if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
               )
            {
                return new RServiceResult<bool>(false, $"duplicated artifact friendly url '{friendlyUrl}'");
            }



            ImportJob job = new ImportJob()
            {
                JobType = JobType.Cambridge,
                ResourceNumber = resourceNumber,
                FriendlyUrl = friendlyUrl,
                SrcUrl = url,
                QueueTime = DateTime.Now,
                ProgressPercent = 0,
                Status = ImportJobStatus.NotStarted
            };


            await _context.ImportJobs.AddAsync
                (
                job
                );

            await _context.SaveChangesAsync();


            try
            {

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {


                                using (var client = new HttpClient())
                                {
                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {
                                            using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                            {
                                                if (
                                                   (await context.Artifacts.Where(a => a.FriendlyUrl == job.FriendlyUrl).SingleOrDefaultAsync())
                                                   !=
                                                   null
                                                  )
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "aborted because of duplicated friendly url";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }
                                                RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from {url}", $"extracted from {url}")
                                                {
                                                    Status = PublishStatus.Draft,
                                                    DateTime = DateTime.Now,
                                                    LastModified = DateTime.Now,
                                                    CoverItemIndex = 0,
                                                    FriendlyUrl = friendlyUrl
                                                };


                                                List<RTagValue> meta = new List<RTagValue>();
                                                RTagValue tag;

                                                string json = await result.Content.ReadAsStringAsync();
                                                var parsed = JObject.Parse(json);
                                                book.Name = book.NameInEnglish =
                                                     parsed.SelectToken("logicalStructures[*].label").Value<string>();

                                                book.Description = book.DescriptionInEnglish =
                                                    Regex.Replace(
                                                     parsed.SelectToken("descriptiveMetadata[*].abstract.displayForm").Value<string>(),
                                                     "<.*?>", string.Empty);

                                                int tagOrder = 1;
                                                foreach (JToken descriptiveMetadata in parsed.SelectTokens("$.descriptiveMetadata[*]").Children())
                                                {
                                                    foreach(JToken child in descriptiveMetadata.Children())
                                                    {
                                                        if (child.SelectToken("label") != null && child.SelectToken("display") != null)
                                                        {
                                                            if (child.SelectToken("display").Value<string>() == "True")
                                                            {
                                                                string metaName = child.SelectToken("label").Value<string>();
                                                                string metaValue = "";
                                                                if (child.SelectToken("displayForm") != null)
                                                                {
                                                                    metaValue = Regex.Replace(
                                                                         child.SelectToken("displayForm").Value<string>(),
                                                                         "<.*?>", string.Empty);
                                                                    tag = await TagHandler.PrepareAttribute(context, metaName, metaValue, tagOrder++);
                                                                    meta.Add(tag);
                                                                }
                                                                else
                                                                    if (child.SelectToken("value") != null)
                                                                {
                                                                    foreach (JToken value in child.SelectTokens("value").Children())
                                                                    {
                                                                        if (value.SelectToken("displayForm") != null)
                                                                        {
                                                                            metaValue = Regex.Replace(
                                                                                 value.SelectToken("displayForm").Value<string>(),
                                                                                 "<.*?>", string.Empty);
                                                                            tag = await TagHandler.PrepareAttribute(context, metaName, metaValue, tagOrder++);
                                                                            meta.Add(tag);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    
                                                }

                                                string imageReproPageURL = "https://image01.cudl.lib.cam.ac.uk";
                                                

                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = json;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }

                                                


                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "University of Cambridge Digital Library", 1);
                                                string viewerUrl = $"http://cudl.lib.cam.ac.uk/view/{resourceNumber}";
                                                tag.ValueSupplement = viewerUrl;

                                                meta.Add(tag);

                                                book.Tags = meta.ToArray();

                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                int order = 0;
                                                foreach (JToken pageToken in parsed.SelectTokens("$.pages").Children())
                                                {
                                                    order++;                                                    

                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.ProgressPercent = order;
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }

                                                    string imageUrl = imageReproPageURL + pageToken.SelectToken("downloadImageURL").Value<string>();

                                                    RArtifactItemRecord page = new RArtifactItemRecord()
                                                    {
                                                        Name = $"تصویر {order}",
                                                        NameInEnglish = $"Image {order}",
                                                        Description = "",
                                                        DescriptionInEnglish = "",
                                                        Order = order,
                                                        FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                                        LastModified = DateTime.Now
                                                    };

                                                    List<RTagValue> pageMata = new List<RTagValue>();
                                                    tag = await TagHandler.PrepareAttribute(context, "Source", "University of Cambridge Digital Library", 1);
                                                    tag.ValueSupplement = $"{viewerUrl}/{order}";
                                                    pageMata.Add(tag);

                                                    if(pageToken.SelectToken("label") != null)
                                                    {
                                                        tag = await TagHandler.PrepareAttribute(context, "Label", pageToken.SelectToken("label").Value<string>(), 1);
                                                        pageMata.Add(tag);
                                                    }


                                                    page.Tags = pageMata.ToArray();

                                                    if (!string.IsNullOrEmpty(imageUrl))
                                                    {
                                                        bool recovered = false;
                                                        if (
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           &&
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           &&
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           )
                                                        {
                                                            RServiceResult<RPictureFile> picture = await _pictureFileService.RecoverFromeFiles(page.Name, page.Description, 1,
                                                                imageUrl,
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                            if (picture.Result != null)
                                                            {
                                                                recovered = true;
                                                                page.Images = new RPictureFile[] { picture.Result };
                                                                page.CoverImageIndex = 0;

                                                                if (book.CoverItemIndex == (order - 1))
                                                                {
                                                                    book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                }

                                                                pages.Add(page);
                                                            }

                                                        }
                                                        if (!recovered)
                                                        {
                                                            if (
                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )

                                                           )
                                                            {
                                                                File.Delete
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               );
                                                            }
                                                            if (

                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )

                                                           )
                                                            {
                                                                File.Delete
                                                                (
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                );
                                                            }
                                                            if (

                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                               )
                                                           )
                                                            {
                                                                File.Delete
                                                                (
                                                                Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                );
                                                            }
                                                            
                                                            var imageResult = await client.GetAsync(imageUrl);

                                                            if (imageResult.StatusCode == HttpStatusCode.NotFound)
                                                            {
                                                                break;//finished
                                                            }


                                                            int _ImportRetryCount = 200;
                                                            int _ImportRetryInitialSleep = 500;
                                                            int retryCount = 0;
                                                            while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                                            {
                                                                imageResult.Dispose();
                                                                Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                                                imageResult = await client.GetAsync(imageUrl);
                                                                retryCount++;
                                                            }

                                                            if (imageResult.IsSuccessStatusCode)
                                                            {
                                                                using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                                                {
                                                                    RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, imageUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                                    if (picture.Result == null)
                                                                    {
                                                                        throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                                                    }



                                                                    page.Images = new RPictureFile[] { picture.Result };
                                                                    page.CoverImageIndex = 0;

                                                                    if (book.CoverItemIndex == (order - 1))
                                                                    {
                                                                        book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                    }

                                                                    pages.Add(page);

                                                                }
                                                            }
                                                            else
                                                            {
                                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                                {
                                                                    job.EndTime = DateTime.Now;
                                                                    job.Status = ImportJobStatus.Failed;
                                                                    job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                    importJobUpdaterDb.Update(job);
                                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                                }

                                                                imageResult.Dispose();
                                                                return;
                                                            }

                                                            imageResult.Dispose();
                                                            GC.Collect();
                                                        }
                                                    }
                                                }


                                                book.Items = pages.ToArray();
                                                book.ItemCount = pages.Count;

                                                if (pages.Count == 0)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "Pages.Count == 0";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                await context.Artifacts.AddAsync(book);
                                                await context.SaveChangesAsync();

                                                job.ProgressPercent = 100;
                                                job.Status = ImportJobStatus.Succeeded;
                                                job.ArtifactId = book.Id;
                                                job.EndTime = DateTime.Now;
                                                context.Update(job);
                                                await context.SaveChangesAsync();


                                            }
                                        }
                                        else
                                        {
                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                            {
                                                job.EndTime = DateTime.Now;
                                                job.Status = ImportJobStatus.Failed;
                                                job.Exception = $"Http result is not ok ({result.StatusCode}) for {url}";
                                                importJobUpdaterDb.Update(job);
                                                await importJobUpdaterDb.SaveChangesAsync();
                                            }
                                            return;
                                        }

                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );



                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// from http://www.bl.uk
        /// </summary>
        /// <param name="resourceNumber">grenville_xli_f001r</param>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromBritishLibrary(string resourceNumber, string friendlyUrl)
        {
            string url = $"http://www.bl.uk/manuscripts/Viewer.aspx?ref={resourceNumber}";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.BritishLibrary && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                    .SingleOrDefaultAsync()
                )
                !=
                null
                )
            {
                return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing {url}");
            }

            if (string.IsNullOrEmpty(friendlyUrl))
            {
                friendlyUrl = resourceNumber;
            }

            if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
               )
            {
                return new RServiceResult<bool>(false, $"duplicated artifact friendly url '{friendlyUrl}'");
            }



            ImportJob job = new ImportJob()
            {
                JobType = JobType.BritishLibrary,
                ResourceNumber = resourceNumber,
                FriendlyUrl = friendlyUrl,
                SrcUrl = url,
                QueueTime = DateTime.Now,
                ProgressPercent = 0,
                Status = ImportJobStatus.NotStarted
            };


            await _context.ImportJobs.AddAsync
                (
                job
                );

            await _context.SaveChangesAsync();


            try
            {

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {


                                using (var client = new HttpClient())
                                {
                                    client.Timeout = TimeSpan.FromMinutes(5);
                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {
                                            using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                            {
                                                if (
                                                   (await context.Artifacts.Where(a => a.FriendlyUrl == job.FriendlyUrl).SingleOrDefaultAsync())
                                                   !=
                                                   null
                                                  )
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "aborted because of duplicated friendly url";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }
                                                RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from {url}", $"extracted from {url}")
                                                {
                                                    Status = PublishStatus.Draft,
                                                    DateTime = DateTime.Now,
                                                    LastModified = DateTime.Now,
                                                    CoverItemIndex = 0,
                                                    FriendlyUrl = friendlyUrl
                                                };


                                                List<RTagValue> meta = new List<RTagValue>();
                                                RTagValue tag;

                                                string html = await result.Content.ReadAsStringAsync();


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = html;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }

                                                
                                                int nIdxStart = html.IndexOf("PageList");
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "PageList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart = html.IndexOf("value=\"", nIdxStart);
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "value after PageList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart += "value=\"".Length;

                                                string strPageList = html.Substring(nIdxStart, html.IndexOf('"', nIdxStart) - nIdxStart);

                                                nIdxStart = html.IndexOf("TextList");
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "TextList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart = html.IndexOf("value=\"", nIdxStart);
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "value after TextList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart += "value=\"".Length;

                                                string strTextList = html.Substring(nIdxStart, html.IndexOf('"', nIdxStart) - nIdxStart);

                                                nIdxStart = html.IndexOf("TitleList");
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "TitleList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart = html.IndexOf("value=\"", nIdxStart);
                                                if (nIdxStart == -1)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "value after TitleList not found";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                nIdxStart += "value=\"".Length;

                                                string strTitleList = html.Substring(nIdxStart, html.IndexOf('"', nIdxStart) - nIdxStart);

                                                string[] PageUrls = strPageList.Split("||", StringSplitOptions.None);
                                                string[] PageTexts = strTextList.Split("||", StringSplitOptions.None);
                                                string[] PageTitles = strTitleList.Split("||", StringSplitOptions.None);

                                                if(PageUrls.Length != PageTexts.Length || PageTexts.Length != PageTitles.Length)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "PageUrls.Length != PageTexts.Length || PageTexts.Length != PageTitles.Length";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                tag = await TagHandler.PrepareAttribute(context, "Title", "Untitled", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Contributor Names", "Unknown", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);

                                                book.Tags = meta.ToArray();


                                                tag = await TagHandler.PrepareAttribute(context, "Source", "British Library", 1);
                                                string viewerUrl = $"http://www.bl.uk/manuscripts/FullDisplay.aspx?ref={resourceNumber.Substring(0, resourceNumber.LastIndexOf('_'))}";
                                                tag.ValueSupplement = viewerUrl;

                                                meta.Add(tag);
                                               
                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                int order = 0;
                                                for(int i = 0; i < PageUrls.Length; i++)
                                                {
                                                    if (PageUrls[i] == "##")
                                                        continue;
                                                    order++;                                                   

                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.ProgressPercent = order;
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }

                                                    
                                                    RArtifactItemRecord page = new RArtifactItemRecord()
                                                    {
                                                        Name = $"تصویر {order}",
                                                        NameInEnglish = $"Image {order}",
                                                        Description = "",
                                                        DescriptionInEnglish = "",
                                                        Order = order,
                                                        FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                                        LastModified = DateTime.Now
                                                    };


                                                    List<RTagValue> pageTags = new List<RTagValue>();
                                                    tag = await TagHandler.PrepareAttribute(context, "Source", "British Library", 1);
                                                    tag.ValueSupplement = $"http://www.bl.uk/manuscripts/Viewer.aspx?ref={PageUrls[i]}";
                                                    pageTags.Add(tag);


                                                    if (!string.IsNullOrEmpty(PageTitles[i]))
                                                    {
                                                        RTagValue toc = await TagHandler.PrepareAttribute(context, "Title in TOC", PageTitles[i], 1);
                                                        toc.ValueSupplement = "1";//font size
                                                        pageTags.Add(toc);
                                                    }

                                                    if (!string.IsNullOrEmpty(PageTexts[i]))
                                                    {
                                                        tag = await TagHandler.PrepareAttribute(context, "Label", PageTexts[i], 1);
                                                        pageTags.Add(tag);
                                                     
                                                    }

                                                    page.Tags = pageTags.ToArray();


                                                    bool recovered = false;
                                                    if (
                                                       File.Exists
                                                       (
                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                       )
                                                       &&
                                                       File.Exists
                                                       (
                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                       )
                                                       &&
                                                       File.Exists
                                                       (
                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                       )
                                                       )
                                                    {
                                                        RServiceResult<RPictureFile> picture = await _pictureFileService.RecoverFromeFiles(page.Name, page.Description, 1,
                                                            viewerUrl,
                                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                            $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                        if (picture.Result != null)
                                                        {
                                                            recovered = true;
                                                            page.Images = new RPictureFile[] { picture.Result };
                                                            page.CoverImageIndex = 0;

                                                            if (book.CoverItemIndex == (order - 1))
                                                            {
                                                                book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                            }

                                                            pages.Add(page);
                                                        }

                                                    }
                                                    if (!recovered)
                                                    {
                                                        if (
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )

                                                       )
                                                        {
                                                            File.Delete
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           );
                                                        }
                                                        if (

                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )

                                                       )
                                                        {
                                                            File.Delete
                                                            (
                                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                            );
                                                        }
                                                        if (

                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                       )
                                                        {
                                                            File.Delete
                                                            (
                                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                            );
                                                        }

                                                        /*
                                                         failed multithread attempt:

                                                            BLTileMixer mixer = new BLTileMixer();
                                                            RServiceResult<Stream> blResult = await mixer.DownloadMix(PageUrls[i], order);
                                                         */


                                                        Dictionary<(int x, int y), Image> tiles = new Dictionary<(int x, int y), Image>(); 
                                                        int max_x = -1;
                                                        for(int x = 0; ;x++)
                                                        {
                                                            string imageUrl = $"http://www.bl.uk/manuscripts/Proxy.ashx?view={PageUrls[i]}_files/13/{x}_0.jpg";
                                                            var imageResult = await client.GetAsync(imageUrl);

                                                            int _ImportRetryCount = 5;
                                                            int _ImportRetryInitialSleep = 500;
                                                            int retryCount = 0;
                                                            while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                                            {
                                                                imageResult.Dispose();
                                                                Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                                                imageResult = await client.GetAsync(imageUrl);
                                                                retryCount++;
                                                            }

                                                            if (imageResult.IsSuccessStatusCode)
                                                            {
                                                                using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                                                {
                                                                    
                                                                    imageStream.Position = 0;
                                                                    try
                                                                    {
                                                                        Image tile = Image.FromStream(imageStream);
                                                                        tiles.Add((x, 0), tile);
                                                                        max_x = x;
                                                                    }
                                                                    catch(Exception aexp)
                                                                    {
                                                                        if (aexp is ArgumentException)
                                                                        {
                                                                            break;
                                                                        }                                                                       
                                                                        throw aexp;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                                {
                                                                    job.EndTime = DateTime.Now;
                                                                    job.Status = ImportJobStatus.Failed;
                                                                    job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                    importJobUpdaterDb.Update(job);
                                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                                }

                                                                imageResult.Dispose();
                                                                return;
                                                            }
                                                        }

                                                        int max_y = -1;
                                                        for (int y = 1; ; y++)
                                                        {
                                                            string imageUrl = $"http://www.bl.uk/manuscripts/Proxy.ashx?view={PageUrls[i]}_files/13/0_{y}.jpg";
                                                            var imageResult = await client.GetAsync(imageUrl);

                                                            int _ImportRetryCount = 5;
                                                            int _ImportRetryInitialSleep = 500;
                                                            int retryCount = 0;
                                                            while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                                            {
                                                                imageResult.Dispose();
                                                                Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                                                imageResult = await client.GetAsync(imageUrl);
                                                                retryCount++;
                                                            }

                                                            if (imageResult.IsSuccessStatusCode)
                                                            {
                                                                using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                                                {
                                                                    if (imageStream.Length <= 248)
                                                                        break;
                                                                    imageStream.Position = 0;
                                                                    try
                                                                    {
                                                                        Image tile = Image.FromStream(imageStream);
                                                                        tiles.Add((0, y), tile);
                                                                        max_y = y;
                                                                    }
                                                                    catch (Exception aexp)
                                                                    {
                                                                        if (aexp is ArgumentException)
                                                                        {
                                                                            break;
                                                                        }
                                                                        throw aexp;
                                                                    }                                                                   
                                                                }
                                                            }
                                                            else
                                                            {
                                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                                {
                                                                    job.EndTime = DateTime.Now;
                                                                    job.Status = ImportJobStatus.Failed;
                                                                    job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                    importJobUpdaterDb.Update(job);
                                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                                }

                                                                imageResult.Dispose();
                                                                return;
                                                            }
                                                        }

                                                        for(int x = 0; x <= max_x; x++)
                                                            for(int y = 0; y <= max_y; y++)
                                                            if(tiles.TryGetValue((x, y), out Image tmp) == false)
                                                                {
                                                                    string imageUrl = $"http://www.bl.uk/manuscripts/Proxy.ashx?view={PageUrls[i]}_files/13/{x}_{y}.jpg";
                                                                    var imageResult = await client.GetAsync(imageUrl);

                                                                    int _ImportRetryCount = 5;
                                                                    int _ImportRetryInitialSleep = 500;
                                                                    int retryCount = 0;
                                                                    while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                                                    {
                                                                        imageResult.Dispose();
                                                                        Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                                                        imageResult = await client.GetAsync(imageUrl);
                                                                        retryCount++;
                                                                    }

                                                                    if (imageResult.IsSuccessStatusCode)
                                                                    {
                                                                        using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                                                        {
                                                                            if (imageStream.Length == 0)
                                                                                break;
                                                                            imageStream.Position = 0;
                                                                            tiles.Add((x, y), Image.FromStream(imageStream));
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                                        {
                                                                            job.EndTime = DateTime.Now;
                                                                            job.Status = ImportJobStatus.Failed;
                                                                            job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                            importJobUpdaterDb.Update(job);
                                                                            await importJobUpdaterDb.SaveChangesAsync();
                                                                        }

                                                                        imageResult.Dispose();
                                                                        return;
                                                                    }
                                                                }

                                                        int tileWidth = tiles[(0, 0)].Width;
                                                        int tileHeight = tiles[(0, 0)].Height;

                                                        int imageWidth = tileWidth * (max_x + 1);
                                                        int imageHeight = tileHeight * (max_y + 1);

                                                        using(Image image = new Bitmap(imageWidth, imageHeight))
                                                        {
                                                            using(Graphics g = Graphics.FromImage(image))
                                                            {
                                                                for(int x = 0; x <= max_x; x++)
                                                                    for(int y = 0; y < max_y; y++)
                                                                    {
                                                                        g.DrawImage(tiles[(x, y)], new Point(x * tileWidth, y * tileHeight));
                                                                        tiles[(x, y)].Dispose();
                                                                    }

                                                                using (Stream imageStream = new MemoryStream())
                                                                {
                                                                    ImageCodecInfo jpgEncoder = _pictureFileService.GetEncoder(ImageFormat.Jpeg);

                                                                    Encoder myEncoder =
                                                                        Encoder.Quality;
                                                                    EncoderParameters jpegParameters = new EncoderParameters(1);

                                                                    EncoderParameter qualityParameter = new EncoderParameter(myEncoder, 92L);
                                                                    jpegParameters.Param[0] = qualityParameter;

                                                                    image.Save(imageStream, jpgEncoder, jpegParameters);
                                                                    RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, $"http://www.bl.uk/manuscripts/Viewer.aspx?ref={PageUrls[i]}", imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                                    if (picture.Result == null)
                                                                    {
                                                                        throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                                                    }

                                                                    page.Images = new RPictureFile[] { picture.Result };
                                                                    page.CoverImageIndex = 0;

                                                                    if (book.CoverItemIndex == (order - 1))
                                                                    {
                                                                        book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                    }

                                                                    pages.Add(page);
                                                                }
                                                            }
                                                        }

                                                        GC.Collect();



                                                    }
                                                }


                                                book.Items = pages.ToArray();
                                                book.ItemCount = pages.Count;

                                                if (pages.Count == 0)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "Pages.Count == 0";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                await context.Artifacts.AddAsync(book);
                                                await context.SaveChangesAsync();

                                                job.ProgressPercent = 100;
                                                job.Status = ImportJobStatus.Succeeded;
                                                job.ArtifactId = book.Id;
                                                job.EndTime = DateTime.Now;
                                                context.Update(job);
                                                await context.SaveChangesAsync();


                                            }
                                        }
                                        else
                                        {
                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                            {
                                                job.EndTime = DateTime.Now;
                                                job.Status = ImportJobStatus.Failed;
                                                job.Exception = $"Http result is not ok ({result.StatusCode}) for {url}";
                                                importJobUpdaterDb.Update(job);
                                                await importJobUpdaterDb.SaveChangesAsync();
                                            }
                                            return;
                                        }

                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );



                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }



        /// <summary>
        /// import from server folder
        /// </summary>
        /// <param name="folderPath">C:\Tools\batches\florence</param>
        /// <param name="friendlyUrl">shahname-florence</param>
        /// <param name="srcUrl">https://t.me/dr_khatibi_abolfazl/888</param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromServerFolder(string folderPath, string friendlyUrl, string srcUrl)
        {
            try
            {
                if (
                    (
                    await _context.ImportJobs
                        .Where(j => j.JobType == JobType.ServerFolder && j.ResourceNumber == folderPath && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                        .SingleOrDefaultAsync()
                    )
                    !=
                    null
                    )
                {
                    return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing server folder {folderPath}");
                }

                if (string.IsNullOrEmpty(friendlyUrl))
                {
                    return new RServiceResult<bool>(false, $"Friendly url is empty, server folder {folderPath}");
                }

                if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
                )
                {
                    return new RServiceResult<bool>(false, $"duplicated friendly url '{friendlyUrl}'");
                }

                ImportJob job = new ImportJob()
                {
                    JobType = JobType.ServerFolder,
                    ResourceNumber = folderPath,
                    FriendlyUrl = friendlyUrl,
                    SrcUrl = srcUrl,
                    QueueTime = DateTime.Now,
                    ProgressPercent = 0,
                    Status = ImportJobStatus.NotStarted
                };


                await _context.ImportJobs.AddAsync
                    (
                    job
                    );

                await _context.SaveChangesAsync();

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {
                                using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                {
                                    RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from server folder {job.ResourceNumber}", $"extracted from server folder {job.ResourceNumber}")
                                    {
                                        Status = PublishStatus.Draft,
                                        DateTime = DateTime.Now,
                                        LastModified = DateTime.Now,
                                        CoverItemIndex = 0,
                                        FriendlyUrl = friendlyUrl
                                    };


                                    List<RTagValue> meta = new List<RTagValue>();
                                    RTagValue tag;


                                    tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                    meta.Add(tag);

                                    tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                    meta.Add(tag);


                                
                                    meta.Add(tag);

                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                    {
                                        job.StartTime = DateTime.Now;
                                        job.Status = ImportJobStatus.Running;
                                        job.SrcContent = "";
                                        importJobUpdaterDb.Update(job);
                                        await importJobUpdaterDb.SaveChangesAsync();
                                    }

                                    List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();
                                    string[] fileNames = Directory.GetFiles(job.ResourceNumber, "*.jpg");
                                    int order = 0;
                                    foreach (string fileName in fileNames)
                                    {
                                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                        {
                                            job.ProgressPercent = order * 100 / (decimal)fileNames.Length;
                                            importJobUpdaterDb.Update(job);
                                            await importJobUpdaterDb.SaveChangesAsync();
                                        }

                                        order++;

                                        RArtifactItemRecord page = new RArtifactItemRecord()
                                        {
                                            Name = $"تصویر {order}",
                                            NameInEnglish = $"Image {order} of {book.NameInEnglish}",
                                            Description = "",
                                            DescriptionInEnglish = "",
                                            Order = order,
                                            FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                            LastModified = DateTime.Now
                                        };

                                       
                                        page.Tags = new RTagValue[] {  };

                                        if (
                                        File.Exists
                                        (
                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                        )

                                                           )
                                        {
                                            File.Delete
                                           (
                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                           );
                                        }
                                        if (

                                           File.Exists
                                           (
                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                           )

                                       )
                                        {
                                            File.Delete
                                            (
                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                            );
                                        }
                                        if (

                                           File.Exists
                                           (
                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                           )
                                       )
                                        {
                                            File.Delete
                                            (
                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                            );
                                        }
                                        using (FileStream imageStream = new FileStream(fileName, FileMode.Open))
                                        {
                                            RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, job.SrcUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                            if (picture.Result == null)
                                            {
                                                throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                            }

                                            page.Images = new RPictureFile[] { picture.Result };

                                            page.CoverImageIndex = 0;

                                            if (book.CoverItemIndex == (order - 1))
                                            {
                                                book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                            }

                                        }


                                        pages.Add(page);
                                    }


                                    book.Tags = meta.ToArray();

                                    book.Items = pages.ToArray();
                                    book.ItemCount = pages.Count;

                                    if (pages.Count == 0)
                                    {
                                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                        {
                                            job.EndTime = DateTime.Now;
                                            job.Status = ImportJobStatus.Failed;
                                            job.Exception = "Pages.Count == 0";
                                            importJobUpdaterDb.Update(job);
                                            await importJobUpdaterDb.SaveChangesAsync();
                                        }
                                        return;
                                    }

                                    await context.Artifacts.AddAsync(book);
                                    await context.SaveChangesAsync();

                                    job.ProgressPercent = 100;
                                    job.Status = ImportJobStatus.Succeeded;
                                    job.ArtifactId = book.Id;
                                    job.EndTime = DateTime.Now;
                                    context.Update(job);
                                    await context.SaveChangesAsync();



                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// from http://www.thedigitalwalters.org/01_ACCESS_WALTERS_MANUSCRIPTS.html
        /// </summary>
        /// <param name="resourceNumber">W619</param>
        /// <param name="friendlyUrl">golestan-walters-01</param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromWalters(string resourceNumber, string friendlyUrl)
        {
            string url = $"http://www.thedigitalwalters.org/Data/WaltersManuscripts/ManuscriptDescriptions/{resourceNumber}_tei.xml";
            if (
                (
                await _context.ImportJobs
                    .Where(j => j.JobType == JobType.Walters && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                    .SingleOrDefaultAsync()
                )
                !=
                null
                )
            {
                return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing {url}");
            }

            if (string.IsNullOrEmpty(friendlyUrl))
            {
                friendlyUrl = resourceNumber;
            }

            if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
               )
            {
                return new RServiceResult<bool>(false, $"duplicated friendly url '{friendlyUrl}'");
            }

            ImportJob job = new ImportJob()
            {
                JobType = JobType.Walters,
                ResourceNumber = resourceNumber,
                FriendlyUrl = friendlyUrl,
                SrcUrl = url,
                QueueTime = DateTime.Now,
                ProgressPercent = 0,
                Status = ImportJobStatus.NotStarted
            };


            await _context.ImportJobs.AddAsync
                (
                job
                );

            await _context.SaveChangesAsync();


            try
            {

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {


                                using (var client = new HttpClient())
                                {

                                    using (var result = await client.GetAsync(url))
                                    {
                                        if (result.IsSuccessStatusCode)
                                        {
                                            using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                            {
                                                RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from {url}", $"extracted from {url}")
                                                {
                                                    Status = PublishStatus.Draft,
                                                    DateTime = DateTime.Now,
                                                    LastModified = DateTime.Now,
                                                    CoverItemIndex = 0,
                                                    FriendlyUrl = friendlyUrl
                                                };


                                                List<RTagValue> meta = new List<RTagValue>();
                                                RTagValue tag;

                                                string xml = await result.Content.ReadAsStringAsync();


                                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                {
                                                    job.StartTime = DateTime.Now;
                                                    job.Status = ImportJobStatus.Running;
                                                    job.SrcContent = xml;
                                                    importJobUpdaterDb.Update(job);
                                                    await importJobUpdaterDb.SaveChangesAsync();
                                                }

                                                XElement elObject = XDocument.Parse(xml).Root;

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                                meta.Add(tag);

                                                tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                                meta.Add(tag);

                                                

                                                


                                                try
                                                {
                                                    foreach (var prop in elObject
                                                        .Elements("{http://www.tei-c.org/ns/1.0}teiHeader").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}fileDesc").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}titleStmt").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}title"))
                                                    {
                                                        string label = prop.Value;
                                                        book.Name = book.NameInEnglish =
                                                            book.Description = book.DescriptionInEnglish =
                                                            label;

                                                        tag = await TagHandler.PrepareAttribute(context, "Title", label, 1);
                                                        meta.Add(tag);
                                                        break;
                                                    }
                                                }
                                                catch
                                                {
                                                    //ignore non-existing = null tags
                                                }

                                                try
                                                {
                                                    foreach (var prop in elObject
                                                        .Elements("{http://www.tei-c.org/ns/1.0}teiHeader").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}fileDesc").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}titleStmt").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}author"))
                                                    {
                                                        string label = prop.Value;
                                                        tag = await TagHandler.PrepareAttribute(context, "Contributor Names", label, 1);
                                                        meta.Add(tag);
                                                        break;
                                                    }
                                                }
                                                catch
                                                {
                                                    //ignore non-existing = null tags
                                                }

                                                try
                                                {
                                                    foreach (var prop in elObject
                                                        .Elements("{http://www.tei-c.org/ns/1.0}teiHeader").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}fileDesc").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}titleStmt").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}respStmt"))
                                                    {
                                                        string label = prop.Elements("{http://www.tei-c.org/ns/1.0}name").First().Value;
                                                        tag = await TagHandler.PrepareAttribute(context, "Contributor Names", label, 1);
                                                        meta.Add(tag);
                                                        break;
                                                        
                                                    }
                                                }
                                                catch
                                                {
                                                    //ignore non-existing = null tags
                                                }

                                                try
                                                {
                                                    foreach (var prop in elObject
                                                        .Elements("{http://www.tei-c.org/ns/1.0}teiHeader").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}fileDesc").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}notesStmt").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}note"))
                                                    {
                                                        string label = prop.Value;
                                                        tag = await TagHandler.PrepareAttribute(context, "Notes", label, 1);
                                                        meta.Add(tag);
                                                    }
                                                }
                                                catch
                                                {
                                                    //ignore non-existing = null tags
                                                }




                                                tag = await TagHandler.PrepareAttribute(context, "Source", "Digitized Walters Manuscripts", 1);
                                                tag.ValueSupplement = $"http://www.thedigitalwalters.org/Data/WaltersManuscripts/html/{job.ResourceNumber}/";

                                                meta.Add(tag);


                                                book.Tags = meta.ToArray();
                                                List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();

                                                int order = 0;

                                                foreach (var surface in elObject
                                                        .Elements("{http://www.tei-c.org/ns/1.0}facsimile").First()
                                                        .Elements("{http://www.tei-c.org/ns/1.0}surface"))
                                                {
                                                    foreach(var graphic in surface.Elements("{http://www.tei-c.org/ns/1.0}graphic"))
                                                    if (graphic.Attribute("url").Value.Contains("sap.jpg"))
                                                    {
                                                        
                                                            order++;

                                                            
                                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                            {
                                                                job.ProgressPercent = order;
                                                                importJobUpdaterDb.Update(job);
                                                                await importJobUpdaterDb.SaveChangesAsync();
                                                            }

                                                            RArtifactItemRecord page = new RArtifactItemRecord()
                                                            {
                                                                Name = $"تصویر {order}",
                                                                NameInEnglish = $"Image {order}",
                                                                Description = "",
                                                                DescriptionInEnglish = "",
                                                                Order = order,
                                                                FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                                                LastModified = DateTime.Now
                                                            };

                                                            

                                                            string imageUrl = $"http://www.thedigitalwalters.org/Data/WaltersManuscripts/{resourceNumber}/data/W.{resourceNumber.Substring(1)}/{graphic.Attribute("url").Value}";

                                                            tag = await TagHandler.PrepareAttribute(context, "Source", "Digitized Walters Manuscripts", 1);
                                                            tag.ValueSupplement = imageUrl;
                                                            page.Tags = new RTagValue[] { tag };

                                                            if (!string.IsNullOrEmpty(imageUrl))
                                                            {
                                                                bool recovered = false;
                                                                if (
                                                                   File.Exists
                                                                   (
                                                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                   )
                                                                   &&
                                                                   File.Exists
                                                                   (
                                                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                   )
                                                                   &&
                                                                   File.Exists
                                                                   (
                                                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                   )
                                                                   )
                                                                {
                                                                    RServiceResult<RPictureFile> picture = await _pictureFileService.RecoverFromeFiles(page.Name, page.Description, 1,
                                                                        imageUrl,
                                                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                                        $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                                    if (picture.Result != null)
                                                                    {
                                                                        recovered = true;
                                                                        page.Images = new RPictureFile[] { picture.Result };
                                                                        page.CoverImageIndex = 0;

                                                                        if (book.CoverItemIndex == (order - 1))
                                                                        {
                                                                            book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                        }

                                                                        pages.Add(page);
                                                                    }

                                                                }
                                                                if (!recovered)
                                                                {
                                                                    if (
                                                                       File.Exists
                                                                       (
                                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                       )

                                                                   )
                                                                    {
                                                                        File.Delete
                                                                       (
                                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                       );
                                                                    }
                                                                    if (

                                                                       File.Exists
                                                                       (
                                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                       )

                                                                   )
                                                                    {
                                                                        File.Delete
                                                                        (
                                                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                        );
                                                                    }
                                                                    if (

                                                                       File.Exists
                                                                       (
                                                                       Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                       )
                                                                   )
                                                                    {
                                                                        File.Delete
                                                                        (
                                                                        Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                                        );
                                                                    }
                                                                    var imageResult = await client.GetAsync(imageUrl);


                                                                    int _ImportRetryCount = 5;
                                                                    int _ImportRetryInitialSleep = 500;
                                                                    int retryCount = 0;
                                                                    while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                                                    {
                                                                        imageResult.Dispose();
                                                                        Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                                                        imageResult = await client.GetAsync(imageUrl);
                                                                        retryCount++;
                                                                    }

                                                                    if (imageResult.IsSuccessStatusCode)
                                                                    {
                                                                        using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                                                        {
                                                                            RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, imageUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                                            if (picture.Result == null)
                                                                            {
                                                                                throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                                                            }

                                                                            page.Images = new RPictureFile[] { picture.Result };
                                                                            page.CoverImageIndex = 0;

                                                                            if (book.CoverItemIndex == (order - 1))
                                                                            {
                                                                                book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                                            }

                                                                            pages.Add(page);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                                        {
                                                                            job.EndTime = DateTime.Now;
                                                                            job.Status = ImportJobStatus.Failed;
                                                                            job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                                            importJobUpdaterDb.Update(job);
                                                                            await importJobUpdaterDb.SaveChangesAsync();
                                                                        }

                                                                        imageResult.Dispose();
                                                                        return;
                                                                    }

                                                                    imageResult.Dispose();
                                                                    GC.Collect();
                                                                }

                                                            }




                                                    }
                                                }

                                                

                                                book.Items = pages.ToArray();
                                                book.ItemCount = pages.Count;

                                                if (pages.Count == 0)
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = "Pages.Count == 0";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }
                                                    return;
                                                }

                                                await context.Artifacts.AddAsync(book);
                                                await context.SaveChangesAsync();

                                                job.ProgressPercent = 100;
                                                job.Status = ImportJobStatus.Succeeded;
                                                job.ArtifactId = book.Id;
                                                job.EndTime = DateTime.Now;
                                                context.Update(job);
                                                await context.SaveChangesAsync();



                                            }
                                        }
                                        else
                                        {
                                            using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                            {
                                                job.EndTime = DateTime.Now;
                                                job.Status = ImportJobStatus.Failed;
                                                job.Exception = $"Http result is not ok ({result.StatusCode}) for {url}";
                                                importJobUpdaterDb.Update(job);
                                                await importJobUpdaterDb.SaveChangesAsync();
                                            }
                                            return;
                                        }

                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );



                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// import from https://viewer.cbl.ie
        /// </summary>
        /// <param name="resourceNumber">119</param>
        /// <param name="friendlyUrl">golestan-baysonghori</param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> StartImportingFromChesterBeatty(string resourceNumber, string friendlyUrl)
        {
            try
            {
                string srcUrl = $"https://viewer.cbl.ie/viewer/object/Per_{resourceNumber}/1/";
                if (
                    (
                    await _context.ImportJobs
                        .Where(j => j.JobType == JobType.ChesterBeatty && j.ResourceNumber == resourceNumber && !(j.Status == ImportJobStatus.Failed || j.Status == ImportJobStatus.Aborted))
                        .SingleOrDefaultAsync()
                    )
                    !=
                    null
                    )
                {
                    return new RServiceResult<bool>(false, $"Job is already scheduled or running for importing {srcUrl}");
                }

                if (string.IsNullOrEmpty(friendlyUrl))
                {
                    return new RServiceResult<bool>(false, $"Friendly url is empty, server folder {srcUrl}");
                }

                if (
                (await _context.Artifacts.Where(a => a.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync())
                !=
                null
                )
                {
                    return new RServiceResult<bool>(false, $"duplicated friendly url '{friendlyUrl}'");
                }

                ImportJob job = new ImportJob()
                {
                    JobType = JobType.ChesterBeatty,
                    ResourceNumber = resourceNumber,
                    FriendlyUrl = friendlyUrl,
                    SrcUrl = srcUrl,
                    QueueTime = DateTime.Now,
                    ProgressPercent = 0,
                    Status = ImportJobStatus.NotStarted
                };


                await _context.ImportJobs.AddAsync
                    (
                    job
                    );

                await _context.SaveChangesAsync();

                _backgroundTaskQueue.QueueBackgroundWorkItem
                    (
                        async token =>
                        {
                            try
                            {
                                using (RMuseumDbContext context = new RMuseumDbContext(Configuration))
                                {
                                    RArtifactMasterRecord book = new RArtifactMasterRecord($"extracted from url {job.ResourceNumber}", $"extracted from url {job.ResourceNumber}")
                                    {
                                        Status = PublishStatus.Draft,
                                        DateTime = DateTime.Now,
                                        LastModified = DateTime.Now,
                                        CoverItemIndex = 0,
                                        FriendlyUrl = friendlyUrl,
                                    };


                                    List<RTagValue> meta = new List<RTagValue>();
                                    RTagValue tag;


                                    tag = await TagHandler.PrepareAttribute(context, "Type", "Book", 1);
                                    meta.Add(tag);

                                    tag = await TagHandler.PrepareAttribute(context, "Type", "Manuscript", 1);
                                    meta.Add(tag);

                                    tag = await TagHandler.PrepareAttribute(context, "Source", "Chester Beatty Digital Collections", 1);
                                    tag.ValueSupplement = srcUrl;

                                    meta.Add(tag);



                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                    {
                                        job.StartTime = DateTime.Now;
                                        job.Status = ImportJobStatus.Running;
                                        job.SrcContent = "";
                                        importJobUpdaterDb.Update(job);
                                        await importJobUpdaterDb.SaveChangesAsync();
                                    }

                                    List<RArtifactItemRecord> pages = new List<RArtifactItemRecord>();
                                    int order = 0;
                                    using (var client = new HttpClient())
                                    do
                                    {


                                        order++;

                                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                        {
                                            job.ProgressPercent = order;
                                            importJobUpdaterDb.Update(job);
                                            await importJobUpdaterDb.SaveChangesAsync();
                                        }

                                        RArtifactItemRecord page = new RArtifactItemRecord()
                                        {
                                            Name = $"تصویر {order}",
                                            NameInEnglish = $"Image {order} of {book.NameInEnglish}",
                                            Description = "",
                                            DescriptionInEnglish = "",
                                            Order = order,
                                            FriendlyUrl = $"p{$"{order}".PadLeft(4, '0')}",
                                            LastModified = DateTime.Now
                                        };

                                        string imageUrl = $"https://viewer.cbl.ie/viewer/rest/image/Per_{resourceNumber}/Per{resourceNumber}_{$"{order}".PadLeft(3, '0')}.jpg/full/!10000,10000/0/default.jpg?ignoreWatermark=true";


                                        page.Tags = new RTagValue[] { };

                                       bool recovered = false;

                                            if (
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           &&
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           &&
                                                           File.Exists
                                                           (
                                                           Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                           )
                                                           )
                                            {
                                                RServiceResult<RPictureFile> picture = await _pictureFileService.RecoverFromeFiles(page.Name, page.Description, 1,
                                                    imageUrl,
                                                    Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                    Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                    Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg"),
                                                    $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                if (picture.Result != null)
                                                {
                                                    recovered = true;
                                                    page.Images = new RPictureFile[] { picture.Result };
                                                    page.CoverImageIndex = 0;

                                                    if (book.CoverItemIndex == (order - 1))
                                                    {
                                                        book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                    }

                                                    tag = await TagHandler.PrepareAttribute(context, "Source", "Chester Beatty Digital Collections", 1);
                                                    tag.ValueSupplement = $"https://viewer.cbl.ie/viewer/object/Per_{resourceNumber}/{$"{order}".PadLeft(3, '0')}/"; ;
                                                    page.Tags = new RTagValue[] { tag };

                                                    pages.Add(page);
                                                }

                                            }

                                            if (!recovered)
                                            {
                                                if (
                                            File.Exists
                                            (
                                            Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                            )

                                                               )
                                                {
                                                    File.Delete
                                                   (
                                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                   );
                                                }
                                                if (

                                                   File.Exists
                                                   (
                                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                   )

                                               )
                                                {
                                                    File.Delete
                                                    (
                                                    Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                    );
                                                }
                                                if (

                                                   File.Exists
                                                   (
                                                   Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                   )
                                               )
                                                {
                                                    File.Delete
                                                    (
                                                    Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(4, '0') + ".jpg")
                                                    );
                                                }
                                                var imageResult = await client.GetAsync(imageUrl);

                                                if (imageResult.StatusCode == HttpStatusCode.Forbidden || imageResult.StatusCode == HttpStatusCode.NotFound)
                                                    break;


                                                int _ImportRetryCount = 5;
                                                int _ImportRetryInitialSleep = 500;
                                                int retryCount = 0;
                                                while (retryCount < _ImportRetryCount && !imageResult.IsSuccessStatusCode && imageResult.StatusCode == HttpStatusCode.ServiceUnavailable)
                                                {
                                                    imageResult.Dispose();
                                                    Thread.Sleep(_ImportRetryInitialSleep * (retryCount + 1));
                                                    imageResult = await client.GetAsync(imageUrl);
                                                    retryCount++;
                                                }

                                                if (imageResult.IsSuccessStatusCode)
                                                {
                                                    using (Stream imageStream = await imageResult.Content.ReadAsStreamAsync())
                                                    {
                                                        RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, imageUrl, imageStream, $"{order}".PadLeft(4, '0') + ".jpg", friendlyUrl);
                                                        if (picture.Result == null)
                                                        {
                                                            throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                                        }

                                                        page.Images = new RPictureFile[] { picture.Result };
                                                        page.CoverImageIndex = 0;

                                                        if (book.CoverItemIndex == (order - 1))
                                                        {
                                                            book.CoverImage = RPictureFile.Duplicate(picture.Result);
                                                        }
                                                        tag = await TagHandler.PrepareAttribute(context, "Source", "Chester Beatty Digital Collections", 1);
                                                        tag.ValueSupplement = $"https://viewer.cbl.ie/viewer/object/Per_{resourceNumber}/{$"{order}".PadLeft(3, '0')}/";
                                                        page.Tags = new RTagValue[] { tag };

                                                        pages.Add(page);
                                                    }
                                                }
                                                else
                                                {
                                                    using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                                    {
                                                        job.EndTime = DateTime.Now;
                                                        job.Status = ImportJobStatus.Failed;
                                                        job.Exception = $"Http result is not ok ({imageResult.StatusCode}) for page {order}, url {imageUrl}";
                                                        importJobUpdaterDb.Update(job);
                                                        await importJobUpdaterDb.SaveChangesAsync();
                                                    }

                                                    imageResult.Dispose();
                                                    return;
                                                }


                                                imageResult.Dispose();
                                                GC.Collect();
                                            }


                                        pages.Add(page);
                                    }
                                    while (true);


                                    book.Tags = meta.ToArray();

                                    book.Items = pages.ToArray();
                                    book.ItemCount = pages.Count;

                                    if (pages.Count == 0)
                                    {
                                        using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                        {
                                            job.EndTime = DateTime.Now;
                                            job.Status = ImportJobStatus.Failed;
                                            job.Exception = "Pages.Count == 0";
                                            importJobUpdaterDb.Update(job);
                                            await importJobUpdaterDb.SaveChangesAsync();
                                        }
                                        return;
                                    }

                                    await context.Artifacts.AddAsync(book);
                                    await context.SaveChangesAsync();

                                    job.ProgressPercent = 100;
                                    job.Status = ImportJobStatus.Succeeded;
                                    job.ArtifactId = book.Id;
                                    job.EndTime = DateTime.Now;
                                    context.Update(job);
                                    await context.SaveChangesAsync();



                                }
                            }
                            catch (Exception exp)
                            {
                                using (RMuseumDbContext importJobUpdaterDb = new RMuseumDbContext(Configuration))
                                {
                                    job.EndTime = DateTime.Now;
                                    job.Status = ImportJobStatus.Failed;
                                    job.Exception = exp.ToString();
                                    importJobUpdaterDb.Update(job);
                                    await importJobUpdaterDb.SaveChangesAsync();
                                }

                            }
                        }
                    );

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
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
                    RUserBookmarkViewModel model = new RUserBookmarkViewModel(bookmark);
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
                        await PushNotification
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
                return new RServiceResult<RUserNoteViewModel>(new RUserNoteViewModel(note, userInfo.Result));
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
                        await PushNotification
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
               

                return new RServiceResult<RUserNoteViewModel>(new RUserNoteViewModel(note, userInfo.Result));
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
                

                return new RServiceResult<RUserNoteViewModel>(new RUserNoteViewModel(note, userInfo.Result));
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
                    res.Add(new RUserNoteViewModel(note, userInfo.Result));

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
                RUserNoteViewModel viewModel = new RUserNoteViewModel(note, new PublicRAppUser(note.RAppUser));
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
                    res.Add(new RUserNoteViewModel(note, userInfo.Result));

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
                RUserNoteViewModel viewModel = new RUserNoteViewModel(note, new PublicRAppUser(note.RAppUser));
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
                    RUserNoteViewModel model = new RUserNoteViewModel(note, user);
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
                    RUserNoteViewModel model = new RUserNoteViewModel(note, new PublicRAppUser(note.RAppUser));
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

                GanjoorLinkViewModel viewModel = new GanjoorLinkViewModel(suggestion, entityName, entityFriendlyUrl, entityImageId);
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
                        new GanjoorLinkViewModel
                        (
                            link,
                            link.Item == null ? link.Artifact.Name : link.Artifact.Name + " » " + link.Item.Name,
                            link.Item == null ? $"/items/{link.Artifact.FriendlyUrl}" : $"/items/{link.Artifact.FriendlyUrl}/{link.Item.FriendlyUrl}",
                            link.Item == null ? link.Artifact.CoverImageId : link.Item.Images.First().Id
                        )
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
                        new PinterestLinkViewModel
                        (
                            link,
                            link.Artifact == null? null : link.Artifact.Name + " » " + link.Item.Name,
                            link.Artifact == null ? null : $"/items/{link.Artifact.FriendlyUrl}/{link.Item.FriendlyUrl}",
                            link.Item == null ? Guid.Empty : link.Item.Images.First().Id
                        )
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
                return new RServiceResult<PinterestLinkViewModel>(new PinterestLinkViewModel(link, null, null, Guid.Empty) );
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
        /// Add Notification
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="subject"></param>
        /// <param name="htmlText"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNotification>> PushNotification(Guid userId, string subject, string htmlText)
        {
            try
            {
                RUserNotification notification =
                            new RUserNotification()
                            {
                                UserId = userId,
                                DateTime = DateTime.Now,
                                Status = NotificationStatus.Unread,
                                Subject = subject,
                                HtmlText = htmlText
                            };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return new RServiceResult<RUserNotification>(notification);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNotification>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Switch Notification Status
        /// </summary>
        /// <param name="notificationId"></param>    
        /// <returns>updated notification object</returns>
        public async Task<RServiceResult<RUserNotification>> SwitchNotificationStatus(Guid notificationId)
        {
            try
            {
                RUserNotification notification =
                            await _context.Notifications.Where(n => n.Id == notificationId).SingleAsync();
                notification.Status = notification.Status == NotificationStatus.Unread ? NotificationStatus.Read : NotificationStatus.Unread;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();               
                return new RServiceResult<RUserNotification>(notification);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNotification>(null, exp.ToString());
            }
        }


        /// <summary>
        /// Delete Notification
        /// </summary>
        /// <param name="notificationId"></param>    
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteNotification(Guid notificationId)
        {
            try
            {
                RUserNotification notification =
                            await _context.Notifications.Where(n => n.Id == notificationId).SingleAsync();               
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Get User Notifications
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNotification[]>> GetUserNotifications(Guid userId)
        {
            try
            {               
                return new RServiceResult<RUserNotification[]>
                    (
                    await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.DateTime)
                    .ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNotification[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get Unread User Notifications Count
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetUnreadUserNotificationsCount(Guid userId)
        {
            try
            {
                return new RServiceResult<int>
                    (
                    await _context.Notifications
                    .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread)
                    .CountAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
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
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="pictureFileService"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="userService"></param>
        public ArtifactService(RMuseumDbContext context, IConfiguration configuration, IPictureFileService pictureFileService, IBackgroundTaskQueue backgroundTaskQueue, IAppUserService userService)
        {
            _context = context;
            _pictureFileService = pictureFileService;
            Configuration = configuration;
            _backgroundTaskQueue = backgroundTaskQueue;
            _userService = userService;
        }
    }
}
