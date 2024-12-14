using Betalgo.Ranul.OpenAI.Managers;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Betalgo.Ranul.OpenAI;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Artifact;
using RSecurityBackend.Services.Implementation;
using System.Collections.Generic;
using System.Net.Http;
using System;
using System.Linq;
using RSecurityBackend.Models.Generic;
using System.IO;
using System.Drawing;
using FluentFTP;
using System.Threading.Tasks;
using RMuseum.Models.GanjoorIntegration;


namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IArtifactService implementation
    /// </summary>
    public partial class ArtifactService : IArtifactService
    {
        public async Task OpenAIStartCreatingImagesForPoemsAsync(int poetId)
        {
            string systemEmail = $"{Configuration.GetSection("Ganjoor")["SystemEmail"]}";
            var systemUserId = (Guid)(await _userService.FindUserByEmail(systemEmail)).Result.Id;

            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>())) //this is long running job, so _context might be already been freed/collected by GC
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIStartCreatingImagesForPoems", "Open AI initialization")).Result;
                      try
                      {

                          var openAiService = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAIAPIKey"],
                              BaseDomain = Configuration["OpenAIBaseUrl"]
                          });

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          string friendlyUrl = "ai";
                          RArtifactMasterRecord book = await context.Artifacts.Include(a => a.CoverImage).Where(b => b.FriendlyUrl == friendlyUrl).SingleOrDefaultAsync();
                          if (book == null)
                          {
                              await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Creating book");
                              book = new RArtifactMasterRecord("فانوس خیال", "تصاویر تولید شده توسط هوش مصنوعی بر اساس متون گنجور")
                              {
                                  Status = PublishStatus.Draft,
                                  DateTime = DateTime.Now,
                                  LastModified = DateTime.Now,
                                  CoverItemIndex = 0,
                                  CoverImageId = Guid.Parse("f9088a13-ea28-4e77-7c84-08dd0fdb87dc"),
                                  FriendlyUrl = friendlyUrl,
                                  NameInEnglish = "AI Generated images from ganjoor.net text",
                                  ItemCount = 0,
                                  Items = new List<RArtifactItemRecord>(),
                                  Tags = new List<RTagValue>(),
                              };
                              await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Creating book - 2");
                              await context.Artifacts.AddAsync(book);
                              await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Creating book - 3");
                          }
                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Query Poems");

                          var poets = await context.GanjoorPoets.AsNoTracking().Where(p => poetId == 0 || p.Id == poetId).OrderBy(p => p.Id).ToListAsync();
                          foreach (var poet in poets)
                          {
                              if (poet.Id < 5) continue;
                              
                              var poems = await context.GanjoorPoems.Include(p => p.Cat).AsNoTracking().Where(p => p.Cat.PoetId == poet.Id).ToListAsync();
                              await jobProgressServiceEF.UpdateJob(job.Id, 0, $"PoetId = {poetId}, Starting, {poems.Count}");
                              for (var i = 0; i < poems.Count; i++)
                              {
                                  var poem = poems[i];

                                  await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {poems.Count} - {poem.FullTitle}");

                                  if (true == await context.GanjoorLinks.Where(l => l.ArtifactId == book.Id && l.GanjoorPostId == poem.Id).AnyAsync())
                                      continue;

                                  if (!string.IsNullOrEmpty(poem.PlainText))
                                  {
                                      await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {poems.Count} - {poem.FullTitle} - 1");
                                      //1
                                      bool hasStories = false;
                                      var hasStoriesResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                                      {
                                          Messages = new List<ChatMessage>
                                        {
                                            ChatMessage.FromSystem(
                                                "آیا در متن زیر داستانی وجود دارد؟ اگر داستانی وجود ندارد 0 و اگر یک یا چند داستان وجود دارد 1 برگردان.."+
                                                "منظور از داستان نقل یک ماجراست و اگر نقل یک ماجرا مثل یک گفتگو یا حادثه یا واقعهٔ تاریخی یا حماسی یا عشقی در آن نیست آن را داستان محسوب نکن. " +
                                                " توضیحات اضافی به متن اضافه نکن. فقط 0 یا 1"
                                        +
                                                    Environment.NewLine
                                                    +
                                                    poem.Title + Environment.NewLine
                                                    + poem.PlainText
                                                    ),
                                        },
                                          Model = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o,
                                      });
                                      if (hasStoriesResult.Successful)
                                      {
                                          string resHasStories = hasStoriesResult.Choices.First().Message.Content;
                                          if (resHasStories.Contains("اکتبر"))
                                          {
                                              resHasStories = "0";
                                          }
                                          hasStories = resHasStories != "0";
                                      }
                                      if (!hasStories)
                                      {
                                          await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {poems.Count} - {poem.FullTitle} - بدون داستان");
                                          continue;
                                      }

                                      //2
                                      await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {poems.Count} - {poem.FullTitle} - 2");
                                      var storyResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                                      {
                                          Messages = new List<ChatMessage>
                                        {
                                            ChatMessage.FromSystem(
                                                "اگر در متن زیر داستانی وجود دارد آن را به نثر ساده بنویس. اگر چند داستان وجود دارد سعی کن همه را به طور خلاصه بیان کنی. متنت حالت نقل داستان داشته باشد و در آن نتیجه‌گیری نکن. اگر داستانی وجود ندارد خالی برگردان و هیچ توضیح اضافه‌ای مثل این که داستانی وجود ندارد نده."+
                                                "منظور از داستان نقل یک ماجراست و اگر نقل یک ماجرا مثل یک گفتگو یا حادثه یا واقعهٔ تاریخی یا حماسی یا عشقی در آن نیست آن را داستان محسوب نکن. " +
                                                " توضیحات اضافی به متن اضافه نکن."
                                                    +
                                                    Environment.NewLine
                                                    +
                                                    poem.Title + Environment.NewLine
                                                    + poem.PlainText
                                                    ),
                                        },
                                          Model = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o,
                                      });
                                      string story = "";
                                      if (storyResult.Successful)
                                      {
                                          story = storyResult.Choices.First().Message.Content;
                                          if (story.Contains("اکتبر"))
                                          {
                                              story = "";
                                          }
                                          story = story.Trim();
                                          if(story.Split(' ').Length < 10 && story.Contains("داستان"))
                                          {
                                              story = "";
                                          }
                                      }

                                      if (string.IsNullOrEmpty(story))
                                      {
                                          await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {poems.Count} - {poem.FullTitle} - بدون متن داستان");
                                          continue;
                                      }

                                      await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {poems.Count} - {poem.FullTitle} - 3");

                                      //3
                                      var depictionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                                      {
                                          Messages = new List<ChatMessage>
                                        {
                                            ChatMessage.FromSystem(
                                                "Create a DALL-E propmt for depicting this story using an photorealistic image with Persian miniature style:"
                                                    +
                                                    Environment.NewLine
                                                    +
                                                    story
                                                    ),
                                        },
                                          Model = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o,
                                      });

                                      string prompt = "";
                                      if (depictionResult.Successful)
                                      {
                                          prompt = depictionResult.Choices.First().Message.Content;
                                          prompt = prompt.Trim();
                                      }

                                      if (string.IsNullOrEmpty(prompt))
                                      {
                                          await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {poems.Count} - {poem.FullTitle} - بدون فرمان");
                                          continue;
                                      }

                                      await jobProgressServiceEF.UpdateJob(job.Id, i, $"{i} از {poems.Count} - {poem.FullTitle} - 4");

                                      //4
                                      var imageCreationResult = await openAiService.Image.CreateImage(new ImageCreateRequest
                                        (
                                        prompt
                                        )
                                      {
                                          Model = "dall-e-3"
                                      }
                                        );
                                      if (imageCreationResult.Successful)
                                      {
                                          string imageUrl = imageCreationResult.Results.Select(u => u.Url).First();

                                          if (!string.IsNullOrEmpty(imageUrl))
                                          {


                                              int order = 1 + await context.Items.Where(i => i.RArtifactMasterRecordId == book.Id).CountAsync();

                                              RArtifactItemRecord page = new RArtifactItemRecord()
                                              {
                                                  Name = $"تصویر {order} - {poem.FullTitle}",
                                                  NameInEnglish = $"Image {order} of {book.NameInEnglish}",
                                                  Description = story,
                                                  DescriptionInEnglish = prompt,
                                                  Order = order,
                                                  FriendlyUrl = $"p{$"{order}".PadLeft(7, '0')}",
                                                  LastModified = DateTime.Now,
                                                  RArtifactMasterRecordId = book.Id,
                                              };

                                              var promptTag = await TagHandler.PrepareAttribute(context, "AI Prompt", prompt, 1);
                                              var ganjoorTag = await TagHandler.PrepareAttribute(context, "Ganjoor Link", poem.FullTitle, 1);
                                              ganjoorTag.ValueSupplement = $"https://ganjoor.net{poem.FullUrl}";
                                              var storyTag = await TagHandler.PrepareAttribute(context, "Story", story, 1);
                                              page.Tags = [promptTag, ganjoorTag, storyTag];

                                              if (
                                                               File.Exists
                                                               (
                                                               Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(7, '0') + ".jpg")
                                                               )

                                                           )
                                              {
                                                  File.Delete
                                                 (
                                                 Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(7, '0') + ".jpg")
                                                 );
                                              }
                                              if (

                                                 File.Exists
                                                 (
                                                 Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(7, '0') + ".jpg")
                                                 )

                                             )
                                              {
                                                  File.Delete
                                                  (
                                                  Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(7, '0') + ".jpg")
                                                  );
                                              }
                                              if (

                                                 File.Exists
                                                 (
                                                 Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(7, '0') + ".jpg")
                                                 )
                                             )
                                              {
                                                  File.Delete
                                                  (
                                                  Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(7, '0') + ".jpg")
                                                  );
                                              }

                                              using (var httpClient = new HttpClient())
                                              {
                                                  var imageData = await httpClient.GetByteArrayAsync(imageUrl);
                                                  using var stream = new MemoryStream(imageData);
                                                  using var originalImage = Image.FromStream(stream);
                                                  using (Stream jpegStream = new MemoryStream())
                                                  {
                                                      originalImage.Save(jpegStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                                                      jpegStream.Seek(0, SeekOrigin.Begin);

                                                      RServiceResult<RPictureFile> picture = await _pictureFileService.Add(page.Name, page.Description, 1, null, imageUrl, jpegStream, $"{order}".PadLeft(7, '0') + ".jpg", friendlyUrl);
                                                      if (picture.Result == null)
                                                      {
                                                          throw new Exception($"_pictureFileService.Add : {picture.ExceptionString}");
                                                      }
                                                      page.Images = [picture.Result];
                                                      page.CoverImageIndex = 0;
                                                  }
                                                  context.Add(page);
                                                  await context.SaveChangesAsync();
                                              }


                                              if (book.ItemCount == 0)
                                              {
                                                  book.CoverImage = RPictureFile.Duplicate(page.Images.First());
                                                  book.Status = PublishStatus.Published;
                                                  book.ItemCount = order;
                                                  context.Update(book);
                                                  await context.SaveChangesAsync();

                                                  var resFTPUpload = await _UploadArtifactToExternalServer(book, context, false);
                                                  if (!string.IsNullOrEmpty(resFTPUpload.ExceptionString))
                                                  {
                                                      await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, $"UploadArtifactToExternalServer: {resFTPUpload.ExceptionString}");
                                                      context.Update(job);
                                                      await context.SaveChangesAsync();
                                                      return;
                                                  }
                                              }
                                              else
                                              {
                                                  book.ItemCount = order;
                                                  context.Update(book);
                                                  await context.SaveChangesAsync();
                                              }

                                              var resUpload = await _UploadArtifactPageToExternalServer(page, context, friendlyUrl, false);
                                              if (resUpload.Result != true)
                                              {
                                                  await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, resUpload.ExceptionString);
                                                  return;
                                              }


                                              File.Delete
                                             (
                                             Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "orig"), $"{order}".PadLeft(7, '0') + ".jpg")
                                             );

                                              File.Delete
                                              (
                                              Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "norm"), $"{order}".PadLeft(7, '0') + ".jpg")
                                              );
                                              File.Delete
                                              (
                                              Path.Combine(Path.Combine(Path.Combine(_pictureFileService.ImageStoragePath, friendlyUrl), "thumb"), $"{order}".PadLeft(7, '0') + ".jpg")
                                              );


                                              GanjoorLink suggestion =
                                               new GanjoorLink()
                                               {
                                                   GanjoorPostId = poem.Id,
                                                   GanjoorTitle = poem.FullTitle,
                                                   GanjoorUrl = $"https://ganjoor.net{poem.FullUrl}",
                                                   ArtifactId = book.Id,
                                                   ItemId = page.Id,
                                                   SuggestedById = systemUserId,
                                                   SuggestionDate = DateTime.Now,
                                                   ReviewResult = ReviewResult.Approved,
                                                   DisplayOnPage = true,
                                                   Synchronized = true,
                                               };

                                              context.GanjoorLinks.Add(suggestion);
                                              await context.SaveChangesAsync();
                                          }
                                      }
                                  }
                              }
                          }






                          await jobProgressServiceEF.UpdateJob(job.Id, 100, "", true);
                      }
                      catch (Exception exp)
                      {
                          await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                      }

                  }
              });

        }

        /// <summary>
        /// upload artifact to external server
        /// </summary>
        /// <param name="item"></param>
        /// <param name="context"></param>
        /// <param name="bookFriendlyUrl"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> _UploadArtifactPageToExternalServer(RArtifactItemRecord item, RMuseumDbContext context, string bookFriendlyUrl, bool skipUpload)
        {
            LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
            var job = (await jobProgressServiceEF.NewJob("_UploadArtifactPageToExternalServer", $"Uploading {item.Name}")).Result;

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
                            if (!skipUpload)
                            {
                                await jobProgressServiceEF.UpdateJob(job.Id, 0, localFilePath);
                                var remoteFilePath = $"{Configuration.GetSection("ExternalFTPServer")["RootPath"]}/images/{bookFriendlyUrl}/{imageSizeString}/{Path.GetFileName(localFilePath)}";
                                await ftpClient.UploadFile(localFilePath, remoteFilePath, createRemoteDir: true);
                            }
                        }
                    }

                    if (!skipUpload)
                    {
                        await ftpClient.Disconnect();
                    }
                    await jobProgressServiceEF.DeleteJob(job.Id);

                }

                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString());
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }
    }
}
