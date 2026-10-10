using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.Managers;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        #region AI people extraction DTOs

        private class AIPersonAppearance
        {
            public int? Couplet { get; set; }
            public string Mention { get; set; }
        }

        private class AIExtractedPerson
        {
            public string Name { get; set; }
            public List<string> Forms { get; set; }
            public string Gender { get; set; }
            public string Description { get; set; }
            public List<AIPersonAppearance> Appearances { get; set; }
        }

        private class AIExtractionResult
        {
            public List<AIExtractedPerson> People { get; set; }
        }

        private class AIChoice
        {
            public int Key { get; set; }
            public int PersonId { get; set; }
            public string Confidence { get; set; }
        }

        private class AIChoiceResult
        {
            public List<AIChoice> Choices { get; set; }
        }

        private class AIRelation
        {
            public string Kind { get; set; }
            public int Person1 { get; set; }
            public int Person2 { get; set; }
            public string Type { get; set; }
            public int? Degree { get; set; }
            public int? Couplet { get; set; }
            public string Quote { get; set; }
        }

        private class AIRelationResult
        {
            public List<AIRelation> Relations { get; set; }
        }

        private class AIPersonRec
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Caption { get; set; }
            public string Key { get; set; }
            public string Loose { get; set; }
            public string[] Tokens { get; set; }
            public string Aliases { get; set; }
            public HashSet<string> AliasKeys { get; set; } = new HashSet<string>();
            public HashSet<string> AliasLoose { get; set; } = new HashSet<string>();
        }

        private class AIPeopleRun
        {
            public RMuseumDbContext Context;
            public OpenAIService Service;
            public string Model;
            public Guid UserId;
            public bool DryRun;
            public GanjoorRelatedPersonService RelationService;
            public StringBuilder Log = new StringBuilder();
            public string LastAIError = "";
            //loose names of new-person suggestions made earlier in this same run
            public HashSet<string> NewPersonKeys = new HashSet<string>();
            public Dictionary<int, int> TagCounts = new Dictionary<int, int>();
            public int PoemsDone, PoemsSkipped, PoemsFailed;
            public int TagsCreated, NewPeople, Deferred, Ambiguous;
            public int RelationsCreated, EvidencesCreated, RelationsSkipped, RelationErrors;
        }

        #endregion

        #region AI people helpers

        private static readonly HashSet<string> AIGenericNameTokens = new HashSet<string>
        {
            "شاه", "پهلوان", "سپهبد", "سالار", "پور", "بن", "پسر", "دختر", "خان", "ملک", "گرد", "جهان", "نامور", "کی", "دیو"
        };

        private static string _AINormName(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return "";
            var sb = new StringBuilder();
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case 'ي':
                    case 'ى':
                        sb.Append('ی');
                        break;
                    case 'ك':
                        sb.Append('ک');
                        break;
                    case 'ۀ':
                    case 'ة':
                        sb.Append('ه');
                        break;
                    case 'أ':
                    case 'إ':
                    case 'آ':
                        sb.Append('ا');
                        break;
                    case '‌':
                    case '‍':
                    case '‎':
                    case '‏':
                    case 'ـ':
                        break;
                    default:
                        if ((ch >= 'ً' && ch <= 'ٟ') || ch == 'ٰ')
                            break;
                        sb.Append(ch);
                        break;
                }
            }
            return Regex.Replace(sb.ToString().Trim(), @"\s+", " ");
        }

        private static string _AILooseName(string normalized)
        {
            var sb = new StringBuilder();
            foreach (char ch in normalized)
            {
                switch (ch)
                {
                    case 'چ': sb.Append('ج'); break;
                    case 'ث': sb.Append('س'); break;
                    case 'ص': sb.Append('س'); break;
                    case 'ذ': sb.Append('ز'); break;
                    case 'ض': sb.Append('ز'); break;
                    case 'ظ': sb.Append('ز'); break;
                    case 'ژ': sb.Append('ز'); break;
                    case 'ط': sb.Append('ت'); break;
                    case 'ح': sb.Append('ه'); break;
                    case 'ق': sb.Append('غ'); break;
                    case 'گ': sb.Append('ک'); break;
                    case 'پ': sb.Append('ب'); break;
                    case 'ؤ': sb.Append('و'); break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }

        private static string[] _AITokens(string loose)
        {
            return loose.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 3 && !AIGenericNameTokens.Contains(t))
                .ToArray();
        }

        private static AIPersonRec _AIMakePersonRec(int id, string name, string description, string caption, string aliases)
        {
            string key = _AINormName(name);
            string loose = _AILooseName(key);
            var rec = new AIPersonRec()
            {
                Id = id,
                Name = name,
                Description = description,
                Caption = caption,
                Key = key,
                Loose = loose.Replace(" ", ""),
                Tokens = _AITokens(loose),
                Aliases = aliases,
            };
            if (!string.IsNullOrWhiteSpace(aliases))
            {
                foreach (var alias in aliases.Split(new[] { '،', ',', '؛', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string aliasKey = _AINormName(alias);
                    if (aliasKey.Length == 0)
                        continue;
                    rec.AliasKeys.Add(aliasKey);
                    rec.AliasLoose.Add(_AILooseName(aliasKey).Replace(" ", ""));
                }
            }
            return rec;
        }

        private static int _AILevenshtein(string a, string b, int max)
        {
            if (Math.Abs(a.Length - b.Length) > max)
                return max + 1;
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++)
                prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                int rowMin = cur[0];
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                    if (cur[j] < rowMin)
                        rowMin = cur[j];
                }
                if (rowMin > max)
                    return max + 1;
                var t = prev; prev = cur; cur = t;
            }
            return prev[b.Length];
        }

        /// <summary>
        /// strong = exact (normalized) name match, weak = spelling variant / shared name token
        /// </summary>
        private static void _AIFindCandidates(List<AIPersonRec> all, IEnumerable<string> forms, Dictionary<int, int> tagCounts, out List<AIPersonRec> strong, out List<AIPersonRec> weak)
        {
            var strongList = new List<AIPersonRec>();
            var weakList = new List<AIPersonRec>();
            foreach (var form in forms.Where(f => !string.IsNullOrWhiteSpace(f)))
            {
                string key = _AINormName(form);
                string loose = _AILooseName(key);
                string looseNoSpace = loose.Replace(" ", "");
                var tokens = _AITokens(loose);
                foreach (var p in all)
                {
                    if (p.Key == key || p.AliasKeys.Contains(key))
                    {
                        if (!strongList.Any(x => x.Id == p.Id))
                            strongList.Add(p);
                        continue;
                    }
                    bool isWeak = p.Loose == looseNoSpace
                        || p.AliasLoose.Contains(looseNoSpace)
                        || (tokens.Length > 0 && p.Tokens.Any(t => tokens.Contains(t)))
                        || (looseNoSpace.Length >= 4 && _AILevenshtein(p.Loose, looseNoSpace, 1) <= 1);
                    if (isWeak && !weakList.Any(x => x.Id == p.Id))
                        weakList.Add(p);
                }
            }
            int Count(AIPersonRec p) => tagCounts.TryGetValue(p.Id, out int c) ? c : 0;
            weak = weakList.Where(w => !strongList.Any(x => x.Id == w.Id)).OrderByDescending(w => Count(w)).Take(6).ToList();
            strong = strongList.OrderByDescending(w => Count(w)).Take(6).ToList();
        }

        private static T _AIParseJson<T>(string content) where T : class
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;
            int s = content.IndexOf('{');
            int e = content.LastIndexOf('}');
            if (s < 0 || e <= s)
                return null;
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content.Substring(s, e - s + 1));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<T> _AIAskJsonAsync<T>(AIPeopleRun run, string prompt) where T : class
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    var result = await run.Service.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                    {
                        Messages = new List<ChatMessage>
                        {
                            ChatMessage.FromUser(prompt),
                        },
                        Model = run.Model,
                    });
                    if (result.Successful)
                    {
                        string content = result.Choices?.FirstOrDefault()?.Message?.Content;
                        var parsed = _AIParseJson<T>(content);
                        if (parsed != null)
                            return parsed;
                        run.LastAIError = "unparsable answer (" + (content == null ? 0 : content.Length) + " chars): " + _AIShort(content, 300);
                    }
                    else
                    {
                        run.LastAIError = "API error: " + result.Error?.Code + " " + result.Error?.Message;
                    }
                }
                catch (TaskCanceledException ex)
                {
                    //a timeout is not transient - asking again just doubles the wait
                    run.LastAIError = "timeout: " + ex.Message;
                    return null;
                }
                catch (Exception ex)
                {
                    run.LastAIError = "exception: " + ex.GetType().Name + " " + ex.Message;
                    //retry once
                }
            }
            return null;
        }

        private static string _AIShort(string s, int max)
        {
            if (string.IsNullOrWhiteSpace(s))
                return "";
            s = s.Trim().Replace("\r", " ").Replace("\n", " ");
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }

        #endregion

        #region AI people prompts

        private const int AIExtractWindowThreshold = 45;
        private const int AIExtractWindowSize = 40;
        private const int AIExtractWindowOverlap = 3;

        private const string AIPeopleExtractPrompt =
@"تو یک متخصص ادبیات حماسی فارسی و شاهنامهٔ فردوسی هستی.

در متن زیر که بخشی از شاهنامهٔ فردوسی است، هر بیت با شمارهٔ خودش در قالب [شماره] مشخص شده است. همهٔ شخصیت‌های نام‌دار (انسان، پادشاه، پهلوان، بانو و موجودات داستانی نام‌دار مانند دیوان) را که در این متن نامشان آمده یا با لقب و عنوان آشکاری به آن‌ها اشاره شده است استخراج کن.

قوانین:
- فقط بر پایهٔ همین متن کار کن و از دانسته‌های بیرونی استفاده نکن.
- حیوانات (مانند اسب‌ها)، مکان‌ها، اقوام و گروه‌ها (مانند ایرانیان و ترکان) و افراد بی‌نام را نیاور.
- هر شخصیت را فقط یک بار و با نام رایجش بیاور و فقط شمارهٔ حداکثر سه بیتِ نخستی را بنویس که در آن‌ها نامش آمده یا مستقیماً به او اشاره شده است (نه همهٔ بیت‌ها؛ فهرست بلند نساز).
- برای هر بیت نقش آن شخصیت را مشخص کن: مقدار Participant یعنی او بخشی از داستان یا زمینهٔ همین بخش است (در رویداد حاضر است، کنشگر است، مخاطب است یا موضوع روایت جاری است)؛ مقدار Allusion یعنی نامش فقط برای یادآوری، مقایسه، تشبیه یا اشاره به گذشته یا آینده آمده است.
- gender را با یکی از مقدارهای Male، Female یا Unknown مشخص کن.
- description یک توضیح بسیار کوتاه (حداکثر ۱۲ کلمه) فقط بر پایهٔ همین متن است؛ اگر چیزی نمی‌توان گفت آن را خالی بگذار.
- forms شکل‌های نوشتاری دیگر همان نام در متن است (در صورت وجود).

خروجی را فقط به صورت یک JSON معتبر و بدون هیچ متن دیگری بنویس، با این ساختار:
{""people"":[{""name"":""..."",""forms"":[""...""],""gender"":""Male"",""description"":""..."",""appearances"":[{""couplet"":12,""mention"":""Participant""}]}]}

متن:
";

        private const string AIPeopleChoosePrompt =
@"تو یک متخصص ادبیات حماسی فارسی و شاهنامهٔ فردوسی هستی.

در متن زیر از شاهنامه هر بیت با شمارهٔ خودش مشخص شده است. پس از متن، فهرستی از نام‌ها آمده که هر کدام چند نامزد دارد؛ نامزدها شخصیت‌هایی هستند که از پیش در پایگاه داده ثبت شده‌اند و ممکن است با آن نام یکی باشند. برای هر نام با توجه به بیت‌های همان نام و فقط بر پایهٔ همین متن تشخیص بده که کدام نامزد همان شخصیت است. اگر هیچ‌کدام نیست، personId را 0 بگذار. اگر مطمئن نیستی confidence را low و در غیر این صورت high بگذار.

خروجی را فقط به صورت یک JSON معتبر و بدون هیچ متن دیگری بنویس، با این ساختار:
{""choices"":[{""key"":1,""personId"":123,""confidence"":""high""}]}

متن:
";

        private const string AIPeopleEpithetPrompt =
@"تو یک متخصص ادبیات حماسی فارسی و شاهنامهٔ فردوسی هستی.

در متن زیر از شاهنامه هر بیت با شمارهٔ خودش مشخص شده است. پس از متن فهرستی از نام‌هایی آمده که در پایگاه داده با هیچ شخصیتی تطبیق داده نشده‌اند؛ سپس فهرست شخصیت‌های شناخته‌شدهٔ پایگاه داده با شناسه، نام‌های دیگر و توضیحشان آمده است.

برای هر نام تشخیص بده که آیا لقب یا نام دیگرِ یکی از شخصیت‌های فهرست است. فقط زمانی personId را بنویس که خودِ متن (مثلاً ذکر پدر یا ماجرای همان شخص در همان بیت‌ها) یا توضیح و نام‌های دیگرِ همان شخصیت در فهرست این را روشن کند؛ از دانسته‌های بیرونی و حدس استفاده نکن. اگر چنین نشانه‌ای نیست یا مطمئن نیستی personId را 0 بگذار. confidence را فقط وقتی high بنویس که کاملاً مطمئن هستی.

خروجی را فقط به صورت یک JSON معتبر و بدون هیچ متن دیگری بنویس، با این ساختار:
{""choices"":[{""key"":1,""personId"":123,""confidence"":""high""}]}

متن:
";

        private const string AIPeopleRelationsPrompt =
@"تو یک متخصص ادبیات حماسی فارسی و شاهنامهٔ فردوسی هستی.

در متن زیر از شاهنامه هر بیت با شمارهٔ خودش مشخص شده است. پس از متن، فهرست شخصیت‌های این بخش با شناسهٔ هر کدام آمده است. وظیفهٔ تو یافتن نسبت‌های خویشاوندی و وابستگی‌هایی است که در همین متن به صراحت میان این شخصیت‌ها بیان شده است.

قوانین:
- فقط بر پایهٔ همین متن کار کن. اگر نسبتی در همین متن بیان نشده است آن را نیاور، حتی اگر از جای دیگری می‌دانی.
- فقط شخصیت‌های فهرست‌شده را با شناسهٔ خودشان به کار ببر.
- couplet شمارهٔ بیتی است که خودِ نسبت در آن بیان شده است (شخصیتِ دیگر ممکن است در بیت‌های پیشین نامبرده شده باشد و در این بیت با ضمیر یا عنوانی مانند «پورش» یا «نیا» به او اشاره شود). quote عبارتی عیناً برگرفته از همان بیت است که نسبت را نشان می‌دهد.
- نوع‌های خویشاوندی (kind برابر family): Parent یعنی person1 پدر یا مادر person2 است؛ Ancestor یعنی person1 نیا (اجداد) person2 است و degree در صورت صراحت درجه (مثلاً ۲ برای پدربزرگ) و در غیر این صورت null؛ Sibling یعنی برادر یا خواهر هستند؛ Spouse یعنی همسر هستند.
- نوع‌های وابستگی (kind برابر affiliation): Minister یعنی person1 وزیر person2 بود؛ Advisor یعنی person1 مشاور person2 بود؛ Courtier یعنی person1 درباری یا ملازم person2 بود؛ Servant یعنی person1 خدمتکار شخصی person2 بود؛ MilitaryCommander یعنی person1 سردار و سپهبد زیر فرمان person2 بود؛ Champion یعنی person1 پهلوان وابسته به person2 بود؛ Successor یعنی person1 پس از person2 جانشین او شد؛ Killer یعنی person1 person2 را کشت؛ Ally (هم‌پیمان)، Rival (دشمن و رقیب پایدار) و Companion (همراه و هم‌رزم) متقارن‌اند و ترتیب مهم نیست.
- فقط مواردی را بیاور که در متن به روشنی آمده است؛ حدس نزن.

خروجی را فقط به صورت یک JSON معتبر و بدون هیچ متن دیگری بنویس، با این ساختار:
{""relations"":[{""kind"":""family"",""person1"":12,""person2"":34,""type"":""Parent"",""degree"":null,""couplet"":7,""quote"":""...""}]}
اگر نسبتی پیدا نشد، {""relations"":[]} را بنویس.

متن:
";

        private static readonly HashSet<PersonAffiliationType> AIAllowedAffiliations = new HashSet<PersonAffiliationType>
        {
            PersonAffiliationType.Minister, PersonAffiliationType.Advisor, PersonAffiliationType.Courtier, PersonAffiliationType.Servant,
            PersonAffiliationType.MilitaryCommander, PersonAffiliationType.Champion, PersonAffiliationType.Successor, PersonAffiliationType.Killer,
            PersonAffiliationType.Ally, PersonAffiliationType.Rival, PersonAffiliationType.Companion,
        };

        private static readonly HashSet<PersonAffiliationType> AISymmetricAffiliations = new HashSet<PersonAffiliationType>
        {
            PersonAffiliationType.Ally, PersonAffiliationType.Rival, PersonAffiliationType.Companion, PersonAffiliationType.Contemporary,
        };

        #endregion

        private class AIResolved
        {
            public AIExtractedPerson Person;
            public AIPersonRec Existing;
            public bool IsNew;
            public bool ConfirmedNew;
            public string Skip;
            public List<AIPersonRec> Candidates = new List<AIPersonRec>();
            public int StrongCount;
        }

        /// <summary>
        /// tag people in poems of a book (and suggest relations/affiliations with evidence) using AI;
        /// everything is registered as unreviewed suggestions of the configured user
        /// </summary>
        /// <param name="masterCatId">book category id (33 = Shahnameh)</param>
        /// <param name="startPoemId">first poem id to process (poems are processed in id order)</param>
        /// <param name="count">0 = all</param>
        /// <param name="dryRun">only report in the job log, write nothing</param>
        public void OpenAIStartSuggestingPoemPeople(int masterCatId, int startPoemId, int count, bool dryRun)
        {
            _backgroundTaskQueue.QueueBackgroundWorkItem
              (
              async token =>
              {
                  using (RMuseumDbContext context = new RMuseumDbContext(new DbContextOptions<RMuseumDbContext>()))
                  {
                      LongRunningJobProgressServiceEF jobProgressServiceEF = new LongRunningJobProgressServiceEF(context);
                      var job = (await jobProgressServiceEF.NewJob($"OpenAIStartSuggestingPoemPeople - cat: {masterCatId} - from poem: {startPoemId} - count: {count} - dryRun: {dryRun}", "Open AI initialization")).Result;
                      var run = new AIPeopleRun() { Context = context, DryRun = dryRun };
                      try
                      {
                          run.Model = Configuration["OpenAIPeopleModel"];
                          if (string.IsNullOrWhiteSpace(run.Model))
                              run.Model = Configuration["OpenAITitleModel"];
                          if (string.IsNullOrWhiteSpace(run.Model))
                          {
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "OpenAIPeopleModel (or OpenAITitleModel) is not set in configuration");
                              return;
                          }
                          string userIdText = Configuration["OpenAIPeopleUserId"];
                          if (string.IsNullOrWhiteSpace(userIdText))
                              userIdText = Configuration["OpenAITitleUserId"];
                          if (!Guid.TryParse(userIdText, out run.UserId))
                          {
                              await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, "OpenAIPeopleUserId (or OpenAITitleUserId) is not set (or invalid) in configuration");
                              return;
                          }

                          run.Service = new OpenAIService(new OpenAIOptions()
                          {
                              ApiKey = Configuration["OpenAIAPIKey"],
                              BaseDomain = Configuration["OpenAIBaseUrl"]
                          }, new System.Net.Http.HttpClient() { Timeout = TimeSpan.FromMinutes(5) });
                          run.RelationService = new GanjoorRelatedPersonService(context, _appUserService, _notificationService);

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, "Query data");

                          var allCats = await context.GanjoorCategories.AsNoTracking().Select(c => new { c.Id, c.ParentId }).ToListAsync();
                          var catIds = new HashSet<int> { masterCatId };
                          bool added = true;
                          while (added)
                          {
                              added = false;
                              foreach (var c in allCats)
                              {
                                  if (c.ParentId != null && catIds.Contains(c.ParentId.Value) && catIds.Add(c.Id))
                                      added = true;
                              }
                          }
                          var catIdList = catIds.ToList();

                          var counts = await (from t in context.PoemGeoDateTags.AsNoTracking()
                                              join p in context.GanjoorPoems.AsNoTracking() on t.PoemId equals p.Id
                                              where t.PersonId != null && catIdList.Contains(p.CatId)
                                              group t by t.PersonId.Value into g
                                              select new { PersonId = g.Key, Count = g.Count() }).ToListAsync();
                          foreach (var c in counts)
                              run.TagCounts[c.PersonId] = c.Count;

                          var poemsQuery = context.GanjoorPoems.AsNoTracking()
                              .Where(p => catIdList.Contains(p.CatId) && p.Id >= startPoemId)
                              .OrderBy(p => p.Id)
                              .Select(p => new { p.Id, p.Title });
                          var poems = count > 0 ? await poemsQuery.Take(count).ToListAsync() : await poemsQuery.ToListAsync();

                          await jobProgressServiceEF.UpdateJob(job.Id, 0, $"Poem count = {poems.Count}");

                          for (int i = 0; i < poems.Count; i++)
                          {
                              var poem = poems[i];
                              await jobProgressServiceEF.UpdateJob(job.Id, (int)(100.0 * i / Math.Max(1, poems.Count)),
                                  $"{i + 1} از {poems.Count} - {poem.Id} - {poem.Title} (برچسب: {run.TagsCreated}، شخص جدید: {run.NewPeople}، نسبت: {run.RelationsCreated}، مستند: {run.EvidencesCreated})");
                              try
                              {
                                  int failedBefore = run.PoemsFailed, skippedBefore = run.PoemsSkipped;
                                  await _AIPeopleProcessPoemAsync(run, poem.Id, poem.Title);
                                  if (run.PoemsFailed == failedBefore && run.PoemsSkipped == skippedBefore)
                                      run.PoemsDone++;
                              }
                              catch (Exception exp)
                              {
                                  run.PoemsFailed++;
                                  run.Log.AppendLine($"[{poem.Id}] {poem.Title}: ERROR {_AIShort(exp.Message, 300)}");
                                  context.ChangeTracker.Clear();
                              }
                          }

                          string summary =
                              $"{(dryRun ? "DRY RUN - " : "")}poems: done {run.PoemsDone}, skipped {run.PoemsSkipped}, failed {run.PoemsFailed}; " +
                              $"person tags: {run.TagsCreated} (new people: {run.NewPeople}); deferred: {run.Deferred}; ambiguous: {run.Ambiguous}; " +
                              $"relations: {run.RelationsCreated}, evidences: {run.EvidencesCreated}, skipped: {run.RelationsSkipped}, errors: {run.RelationErrors}";
                          string logText = run.Log.ToString();
                          if (logText.Length > 30000)
                              logText = logText.Substring(logText.Length - 30000);
                          await jobProgressServiceEF.UpdateJob(job.Id, 100, summary + Environment.NewLine + logText, true);
                      }
                      catch (Exception exp)
                      {
                          await jobProgressServiceEF.UpdateJob(job.Id, 100, "", false, exp.ToString() + Environment.NewLine + run.Log.ToString());
                      }
                  }
              });
        }

        private static PersonMentionKind _AIMentionOf(string mention)
        {
            return string.Equals((mention ?? "").Trim(), "Allusion", StringComparison.OrdinalIgnoreCase) ? PersonMentionKind.Allusion : PersonMentionKind.Participant;
        }

        private static List<AIExtractedPerson> _AIMergeExtracted(List<AIExtractedPerson> people, ICollection<int> validCouplets)
        {
            var merged = new Dictionary<string, AIExtractedPerson>();
            foreach (var p in people ?? new List<AIExtractedPerson>())
            {
                if (p == null || string.IsNullOrWhiteSpace(p.Name))
                    continue;
                string key = _AINormName(p.Name);
                if (!merged.TryGetValue(key, out var target))
                {
                    target = new AIExtractedPerson()
                    {
                        Name = p.Name.Trim(),
                        Forms = new List<string>(),
                        Gender = p.Gender,
                        Description = p.Description,
                        Appearances = new List<AIPersonAppearance>(),
                    };
                    merged[key] = target;
                }
                if (p.Forms != null)
                    target.Forms.AddRange(p.Forms.Where(f => !string.IsNullOrWhiteSpace(f)));
                if (string.IsNullOrWhiteSpace(target.Description))
                    target.Description = p.Description;
                foreach (var a in p.Appearances ?? new List<AIPersonAppearance>())
                {
                    if (a?.Couplet == null || !validCouplets.Contains(a.Couplet.Value))
                        continue;
                    if (target.Appearances.Any(x => x.Couplet == a.Couplet))
                        continue;
                    target.Appearances.Add(a);
                }
            }
            return merged.Values.Where(p => p.Appearances.Count > 0).ToList();
        }

        private async Task _AIPeopleProcessPoemAsync(AIPeopleRun run, int poemId, string poemTitle)
        {
            var ctx = run.Context;
            string tag = $"[{poemId}] {poemTitle}";

            //never clobber a pending suggestion of this user on the poem (review it first, then run again)
            if (await ctx.GanjoorPoemCorrections.AsNoTracking().AnyAsync(c => c.PoemId == poemId && c.UserId == run.UserId && !c.Reviewed))
            {
                run.PoemsSkipped++;
                run.Log.AppendLine($"{tag}: skipped (a pending suggestion of this user exists)");
                return;
            }

            var verses = await ctx.GanjoorVerses.AsNoTracking()
                .Where(v => v.PoemId == poemId && v.CoupletIndex != null)
                .OrderBy(v => v.VOrder)
                .Select(v => new { CoupletIndex = v.CoupletIndex.Value, v.Text })
                .ToListAsync();
            var orderedCouplets = verses
                .GroupBy(v => v.CoupletIndex)
                .OrderBy(g => g.Key)
                .Select(g => new KeyValuePair<int, string>(g.Key, string.Join(" / ", g.Select(x => (x.Text ?? "").Trim()))))
                .ToList();
            if (orderedCouplets.Count == 0)
            {
                run.PoemsSkipped++;
                run.Log.AppendLine($"{tag}: skipped (no couplets)");
                return;
            }
            var couplets = orderedCouplets.ToDictionary(c => c.Key, c => c.Value);
            var poemText = new StringBuilder();
            foreach (var c in orderedCouplets)
                poemText.AppendLine($"[{c.Key}] {c.Value}");

            // ---- 1. extraction
            // long poems are asked in overlapping windows of couplets: one huge answer can take longer
            // than any timeout, several smaller ones are quick. The people of all windows are merged by name.
            var windows = new List<string>();
            if (orderedCouplets.Count <= AIExtractWindowThreshold)
            {
                windows.Add(poemText.ToString());
            }
            else
            {
                for (int start = 0; start < orderedCouplets.Count; start += AIExtractWindowSize - AIExtractWindowOverlap)
                {
                    var wsb = new StringBuilder();
                    foreach (var c in orderedCouplets.Skip(start).Take(AIExtractWindowSize))
                        wsb.AppendLine($"[{c.Key}] {c.Value}");
                    windows.Add(wsb.ToString());
                    if (start + AIExtractWindowSize >= orderedCouplets.Count)
                        break;
                }
            }
            var allExtractedPeople = new List<AIExtractedPerson>();
            var extractionTimer = System.Diagnostics.Stopwatch.StartNew();
            for (int w = 0; w < windows.Count; w++)
            {
                var extraction = await _AIAskJsonAsync<AIExtractionResult>(run, AIPeopleExtractPrompt + windows[w]);
                if (extraction == null)
                {
                    run.PoemsFailed++;
                    string part = windows.Count > 1 ? $" (part {w + 1}/{windows.Count})" : "";
                    run.Log.AppendLine($"{tag}: AI extraction failed{part} - {run.LastAIError}");
                    return;
                }
                if (extraction.People != null)
                    allExtractedPeople.AddRange(extraction.People);
            }
            run.Log.AppendLine($"{tag}: extraction took {extractionTimer.Elapsed.TotalSeconds:0}s ({windows.Count} part(s), {allExtractedPeople.Count} people)");
            var extracted = _AIMergeExtracted(allExtractedPeople, couplets.Keys);
            if (extracted.Count == 0)
            {
                run.Log.AppendLine($"{tag}: no people found");
                return;
            }

            // ---- 2. resolution against existing / pending people
            var all = (await ctx.GanjoorRelatedPersons.AsNoTracking().Select(p => new { p.Id, p.Name, p.Description, p.FamilyTreeCaption, p.Aliases }).ToListAsync())
                .Select(p => _AIMakePersonRec(p.Id, p.Name, p.Description, p.FamilyTreeCaption, p.Aliases)).ToList();

            var pendingLoose = new HashSet<string>();
            var pendingJson = await ctx.GanjoorPoemCorrections.AsNoTracking().Where(c => !c.Reviewed)
                .SelectMany(c => c.GeoDateTags).Where(t => t.SuggestedPersonGraphJson != null && t.PersonId == null)
                .Select(t => t.SuggestedPersonGraphJson).ToListAsync();
            foreach (var json in pendingJson)
            {
                var graph = _AIParseJson<PersonGraphSuggestion>(json);
                if (graph == null)
                    continue;
                var nodes = new List<PersonGraphNode>();
                if (graph.Person != null) nodes.Add(graph.Person);
                if (graph.RelatedPeople != null) nodes.AddRange(graph.RelatedPeople);
                foreach (var n in nodes.Where(n => n.ExistingPersonId == null && !string.IsNullOrWhiteSpace(n.Name)))
                    pendingLoose.Add(_AILooseName(_AINormName(n.Name)).Replace(" ", ""));
            }

            var existingTags = new HashSet<(int, int)>((await ctx.PoemGeoDateTags.AsNoTracking()
                .Where(t => t.PoemId == poemId && t.PersonId != null)
                .Select(t => new { PersonId = t.PersonId.Value, t.CoupletIndex }).ToListAsync())
                .Select(t => (t.PersonId, t.CoupletIndex)));
            var pendingTags = new HashSet<(int, int)>((await ctx.GanjoorPoemCorrections.AsNoTracking()
                .Where(c => c.PoemId == poemId && !c.Reviewed)
                .SelectMany(c => c.GeoDateTags).Where(t => t.PersonId != null && !t.MarkForDelete)
                .Select(t => new { PersonId = t.PersonId.Value, t.CoupletIndex }).ToListAsync())
                .Select(t => (t.PersonId, t.CoupletIndex ?? 0)));

            var resolved = new List<AIResolved>();
            foreach (var p in extracted)
            {
                var r = new AIResolved() { Person = p };
                var forms = new List<string>() { p.Name };
                forms.AddRange(p.Forms);
                _AIFindCandidates(all, forms, run.TagCounts, out var strong, out var weak);
                r.StrongCount = strong.Count;
                r.Candidates.AddRange(strong);
                r.Candidates.AddRange(weak);
                if (strong.Count == 1)
                    r.Existing = strong[0];
                else if (r.Candidates.Count == 0)
                    r.IsNew = true;
                resolved.Add(r);
            }

            var toAsk = resolved.Where(r => r.Existing == null && !r.IsNew).ToList();
            if (toAsk.Count > 0)
            {
                var sb = new StringBuilder(AIPeopleChoosePrompt);
                sb.AppendLine(poemText.ToString());
                sb.AppendLine("فهرست نام‌ها:");
                for (int i = 0; i < toAsk.Count; i++)
                {
                    var r = toAsk[i];
                    sb.AppendLine($"نام شمارهٔ {i + 1}: «{r.Person.Name}» - توضیح: {_AIShort(r.Person.Description, 120)} - بیت‌ها: {string.Join("، ", r.Person.Appearances.Select(a => a.Couplet))}");
                    foreach (var cand in r.Candidates)
                        sb.AppendLine($"  نامزد {cand.Id}: {cand.Name}{(string.IsNullOrWhiteSpace(cand.Aliases) ? "" : " (نام‌های دیگر: " + _AIShort(cand.Aliases, 80) + ")")} - {_AIShort(cand.Description, 100)} {_AIShort(cand.Caption, 60)}");
                }
                var answer = await _AIAskJsonAsync<AIChoiceResult>(run, sb.ToString());
                for (int i = 0; i < toAsk.Count; i++)
                {
                    var r = toAsk[i];
                    var choice = answer?.Choices?.FirstOrDefault(c => c.Key == i + 1);
                    if (choice == null || string.Equals(choice.Confidence, "low", StringComparison.OrdinalIgnoreCase))
                    {
                        r.Skip = "ambiguous (" + string.Join(", ", r.Candidates.Select(c => c.Id)) + ")";
                        continue;
                    }
                    var picked = r.Candidates.FirstOrDefault(c => c.Id == choice.PersonId);
                    if (picked != null)
                        r.Existing = picked;
                    else if (choice.PersonId == 0)
                    {
                        r.IsNew = true;
                        r.ConfirmedNew = r.StrongCount > 0;
                    }
                    else
                        r.Skip = "ambiguous (invalid choice)";
                }
            }

            // ---- 2b. names without any lexical candidate may still be an epithet of a well known person
            // (e.g. «تهمتن» for رستم): show the model the people most tagged in this book, with the
            // descriptions and other names stored in the database, and let it link a name only when
            // the poem text or those stored descriptions make it clear
            var unmatched = resolved.Where(q => q.IsNew && q.Skip == null && q.Candidates.Count == 0).ToList();
            if (unmatched.Count > 0)
            {
                var shortlist = all.Where(a => run.TagCounts.ContainsKey(a.Id))
                    .OrderByDescending(a => run.TagCounts[a.Id])
                    .Take(200)
                    .ToList();
                if (shortlist.Count > 0)
                {
                    var sb2 = new StringBuilder(AIPeopleEpithetPrompt);
                    sb2.AppendLine(poemText.ToString());
                    sb2.AppendLine("نام‌های بی‌تطبیق:");
                    for (int i = 0; i < unmatched.Count; i++)
                    {
                        var r = unmatched[i];
                        sb2.AppendLine($"نام شمارهٔ {i + 1}: «{r.Person.Name}»{(r.Person.Forms.Count > 0 ? " (شکل‌های دیگر: " + string.Join("، ", r.Person.Forms.Take(4)) + ")" : "")} - توضیح: {_AIShort(r.Person.Description, 120)} - بیت‌ها: {string.Join("، ", r.Person.Appearances.Select(a => a.Couplet))}");
                    }
                    sb2.AppendLine();
                    sb2.AppendLine("شخصیت‌های شناخته‌شدهٔ پایگاه داده:");
                    foreach (var k in shortlist)
                        sb2.AppendLine($"{k.Id}: {k.Name}{(string.IsNullOrWhiteSpace(k.Aliases) ? "" : " (نام‌های دیگر: " + _AIShort(k.Aliases, 80) + ")")}{(string.IsNullOrWhiteSpace(k.Description) ? "" : " - " + _AIShort(k.Description, 70))}");
                    var epithetAnswer = await _AIAskJsonAsync<AIChoiceResult>(run, sb2.ToString());
                    for (int i = 0; i < unmatched.Count; i++)
                    {
                        var r = unmatched[i];
                        var choice = epithetAnswer?.Choices?.FirstOrDefault(c => c.Key == i + 1);
                        if (choice == null || choice.PersonId == 0 || !string.Equals(choice.Confidence, "high", StringComparison.OrdinalIgnoreCase))
                            continue;
                        var picked = shortlist.FirstOrDefault(k => k.Id == choice.PersonId);
                        if (picked == null)
                            continue;
                        r.Existing = picked;
                        r.IsNew = false;
                        run.Log.AppendLine($"{tag}: «{r.Person.Name}» -> {picked.Id} {picked.Name} (epithet)");
                    }
                }
            }

            foreach (var r in resolved.Where(q => q.IsNew && q.Skip == null))
            {
                var keys = new List<string>() { r.Person.Name };
                keys.AddRange(r.Person.Forms);
                if (keys.Any(k => pendingLoose.Contains(_AILooseName(_AINormName(k)).Replace(" ", "")) || run.NewPersonKeys.Contains(_AILooseName(_AINormName(k)).Replace(" ", ""))))
                    r.Skip = "deferred (same name is a pending new person suggestion - run again after reviewing it)";
            }

            // ---- 3. person tags
            var newTags = new List<GanjoorPoemGeoDateTagCorrection>();
            var seen = new HashSet<(int, int)>();
            int localKey = 0;
            foreach (var r in resolved)
            {
                var p = r.Person;
                string desc = _AIShort(p.Description, 150);
                string note = ("پیشنهاد هوش مصنوعی (" + run.Model + ")" + (desc.Length > 0 ? ": " + desc : ""));
                if (r.Skip != null)
                {
                    if (r.Skip.StartsWith("ambiguous")) run.Ambiguous++; else run.Deferred++;
                    run.Log.AppendLine($"{tag}: «{p.Name}» {r.Skip}");
                    continue;
                }
                var appearances = p.Appearances.OrderBy(a => a.Couplet).ToList();
                if (r.Existing != null)
                {
                    if (_AINormName(p.Name) != r.Existing.Key)
                        note += " (نام در متن: " + p.Name.Trim() + ")";
                    // only the first couplet of the poem is tagged for each person; a person who already
                    // has a tag (or a pending tag) anywhere in this poem is skipped altogether
                    foreach (var a in appearances.Take(1))
                    {
                        var key = (r.Existing.Id, a.Couplet.Value);
                        if (existingTags.Any(t => t.Item1 == r.Existing.Id) || pendingTags.Any(t => t.Item1 == r.Existing.Id) || !seen.Add((r.Existing.Id, 0)))
                            continue;
                        newTags.Add(new GanjoorPoemGeoDateTagCorrection()
                        {
                            CoupletIndex = a.Couplet,
                            PersonId = r.Existing.Id,
                            PersonMention = _AIMentionOf(a.Mention),
                            SuggestionNote = note,
                        });
                        run.Log.AppendLine($"{tag}: tag «{p.Name}» -> {r.Existing.Id} {r.Existing.Name} @ {a.Couplet} ({_AIMentionOf(a.Mention)})");
                    }
                }
                else if (r.IsNew)
                {
                    var first = appearances.First();
                    localKey++;
                    var graph = new PersonGraphSuggestion()
                    {
                        Person = new PersonGraphNode()
                        {
                            LocalKey = "p" + localKey,
                            Name = p.Name.Trim(),
                            Description = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description.Trim(),
                            Gender = new[] { "Male", "Female" }.FirstOrDefault(g => string.Equals(g, (p.Gender ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) ?? "Unknown",
                            Importance = "Normal",
                            ConfirmedNewDespiteNameMatch = r.ConfirmedNew,
                        },
                        RelatedPeople = new List<PersonGraphNode>(),
                        Relations = new List<PersonGraphRelationEntry>(),
                    };
                    newTags.Add(new GanjoorPoemGeoDateTagCorrection()
                    {
                        CoupletIndex = first.Couplet,
                        PersonMention = _AIMentionOf(first.Mention),
                        SuggestedPersonGraphJson = Newtonsoft.Json.JsonConvert.SerializeObject(graph, new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() }),
                        SuggestionNote = note,
                    });
                    run.NewPeople++;
                    run.NewPersonKeys.Add(_AILooseName(_AINormName(p.Name)).Replace(" ", ""));
                    run.Log.AppendLine($"{tag}: NEW person «{p.Name}» @ {first.Couplet} ({_AIMentionOf(first.Mention)})");
                }
            }

            if (newTags.Count > 0 && !run.DryRun)
            {
                ctx.GanjoorPoemCorrections.Add(new GanjoorPoemCorrection()
                {
                    PoemId = poemId,
                    UserId = run.UserId,
                    OriginalTitle = poemTitle,
                    Note = $"برچسب‌های شخصیت پیشنهادی هوش مصنوعی ({run.Model})",
                    Date = DateTime.Now,
                    Result = CorrectionReviewResult.NotReviewed,
                    Reviewed = false,
                    AffectedThePoem = false,
                    HideMyName = false,
                    GeoDateTags = newTags,
                });
                await ctx.SaveChangesAsync();
            }
            run.TagsCreated += newTags.Count;
            foreach (var r in resolved.Where(q => q.Existing != null && q.Skip == null))
            {
                run.TagCounts.TryGetValue(r.Existing.Id, out int c);
                run.TagCounts[r.Existing.Id] = c + 1;
            }

            // ---- 4. relations and affiliations among people that already exist
            var peopleInPoem = resolved.Where(r => r.Existing != null && r.Skip == null).Select(r => r.Existing).GroupBy(x => x.Id).Select(g => g.First()).ToList();
            if (peopleInPoem.Count >= 2)
                await _AIPeopleRelationsAsync(run, poemId, tag, poemText.ToString(), couplets, peopleInPoem);
        }

        private async Task _AIPeopleRelationsAsync(AIPeopleRun run, int poemId, string tag, string poemText, Dictionary<int, string> couplets, List<AIPersonRec> people)
        {
            var ctx = run.Context;
            var sb = new StringBuilder(AIPeopleRelationsPrompt);
            sb.AppendLine(poemText);
            sb.AppendLine("شخصیت‌ها:");
            foreach (var p in people)
                sb.AppendLine($"{p.Id}: {p.Name}{(string.IsNullOrWhiteSpace(p.Description) ? "" : " - " + _AIShort(p.Description, 80))}");

            var result = await _AIAskJsonAsync<AIRelationResult>(run, sb.ToString());
            if (result?.Relations == null || result.Relations.Count == 0)
                return;

            var peopleById = people.ToDictionary(p => p.Id);
            var handled = new HashSet<string>();
            foreach (var rel in result.Relations)
            {
                string label = $"{rel.Kind}/{rel.Type} {rel.Person1}->{rel.Person2} @ {rel.Couplet}";
                try
                {
                    // ---- validation of the AI answer (the poem text is the only allowed source)
                    if (!peopleById.ContainsKey(rel.Person1) || !peopleById.ContainsKey(rel.Person2) || rel.Person1 == rel.Person2)
                    {
                        run.RelationsSkipped++; run.Log.AppendLine($"{tag}: relation rejected (unknown people) {label}"); continue;
                    }
                    if (rel.Couplet == null || !couplets.TryGetValue(rel.Couplet.Value, out string coupletText))
                    {
                        run.RelationsSkipped++; run.Log.AppendLine($"{tag}: relation rejected (invalid couplet) {label}"); continue;
                    }
                    string quote = Regex.Replace(_AINormName(rel.Quote ?? ""), @"[^\p{L}]", "");
                    string coupletLetters = Regex.Replace(_AINormName(coupletText), @"[^\p{L}]", "");
                    if (quote.Length < 4 || !coupletLetters.Contains(quote))
                    {
                        run.RelationsSkipped++; run.Log.AppendLine($"{tag}: relation rejected (quote is not in the couplet) {label}"); continue;
                    }

                    bool isAffiliation = string.Equals(rel.Kind, "affiliation", StringComparison.OrdinalIgnoreCase);
                    PersonRelationType relationType = PersonRelationType.Parent;
                    PersonAffiliationType affiliationType = PersonAffiliationType.Other;
                    bool symmetric;
                    if (isAffiliation)
                    {
                        if (!Enum.TryParse(rel.Type, true, out affiliationType) || !AIAllowedAffiliations.Contains(affiliationType))
                        {
                            run.RelationsSkipped++; run.Log.AppendLine($"{tag}: relation rejected (type) {label}"); continue;
                        }
                        symmetric = AISymmetricAffiliations.Contains(affiliationType);
                    }
                    else
                    {
                        if (!Enum.TryParse(rel.Type, true, out relationType) || !Enum.IsDefined(typeof(PersonRelationType), relationType))
                        {
                            run.RelationsSkipped++; run.Log.AppendLine($"{tag}: relation rejected (type) {label}"); continue;
                        }
                        symmetric = relationType == PersonRelationType.Sibling || relationType == PersonRelationType.Spouse;
                    }

                    int p1 = rel.Person1, p2 = rel.Person2;
                    string handledKey = $"{(isAffiliation ? "A" : "F")}{(isAffiliation ? (int)affiliationType : (int)relationType)}:{(symmetric ? Math.Min(p1, p2) : p1)}:{(symmetric ? Math.Max(p1, p2) : p2)}:{rel.Couplet}";
                    if (!handled.Add(handledKey))
                        continue;

                    string suggestionNote = $"پیشنهاد هوش مصنوعی ({run.Model}) - عبارت: «{_AIShort(rel.Quote, 200)}»";
                    var suggestion = new GanjoorPersonRelationEditSuggestion()
                    {
                        Kind = isAffiliation ? PersonRelationSuggestionKind.Affiliation : PersonRelationSuggestionKind.Family,
                        Person1Id = p1,
                        Person2Id = p2,
                        SuggestedRelationType = relationType,
                        SuggestedAffiliationType = isAffiliation ? affiliationType : (PersonAffiliationType?)null,
                        SuggestedDegreeHint = (!isAffiliation && relationType == PersonRelationType.Ancestor && rel.Degree != null && rel.Degree >= 2) ? rel.Degree : null,
                        EvidencePoemId = poemId,
                        EvidenceCoupletIndex = rel.Couplet,
                        SuggestionNote = suggestionNote,
                        UserId = run.UserId,
                    };

                    // ---- does the edge already exist (live)?
                    int? existingId;
                    if (isAffiliation)
                    {
                        existingId = await ctx.GanjoorPersonAffiliations.AsNoTracking()
                            .Where(a => a.AffiliationType == affiliationType && ((a.Person1Id == p1 && a.Person2Id == p2) || (symmetric && a.Person1Id == p2 && a.Person2Id == p1)))
                            .Select(a => (int?)a.Id).FirstOrDefaultAsync();
                    }
                    else
                    {
                        existingId = await ctx.GanjoorPersonRelations.AsNoTracking()
                            .Where(r => r.RelationType == relationType && ((r.Person1Id == p1 && r.Person2Id == p2) || (symmetric && r.Person1Id == p2 && r.Person2Id == p1)))
                            .Select(r => (int?)r.Id).FirstOrDefaultAsync();
                    }

                    var kind = suggestion.Kind;
                    if (existingId != null)
                    {
                        bool evidenceExists = isAffiliation
                            ? await ctx.GanjoorPersonAffiliationEvidences.AsNoTracking().AnyAsync(e => e.AffiliationId == existingId && e.PoemId == poemId && e.CoupletIndex == rel.Couplet)
                            : await ctx.GanjoorPersonRelationEvidences.AsNoTracking().AnyAsync(e => e.RelationId == existingId && e.PoemId == poemId && e.CoupletIndex == rel.Couplet);
                        bool pendingEvidence = await ctx.GanjoorPersonRelationEditSuggestions.AsNoTracking().AnyAsync(s => !s.Reviewed
                            && s.Action == PersonRelationSuggestionAction.AddEvidence && s.Kind == kind
                            && (isAffiliation ? s.ExistingAffiliationId == existingId : s.ExistingRelationId == existingId)
                            && s.EvidencePoemId == poemId && s.EvidenceCoupletIndex == rel.Couplet);
                        if (evidenceExists || pendingEvidence)
                        {
                            run.RelationsSkipped++;
                            continue;
                        }
                        suggestion.Action = PersonRelationSuggestionAction.AddEvidence;
                        if (isAffiliation) suggestion.ExistingAffiliationId = existingId; else suggestion.ExistingRelationId = existingId;
                    }
                    else
                    {
                        bool pendingAdd = await ctx.GanjoorPersonRelationEditSuggestions.AsNoTracking().AnyAsync(s => !s.Reviewed
                            && s.Action == PersonRelationSuggestionAction.Add && s.Kind == kind
                            && (isAffiliation ? s.SuggestedAffiliationType == affiliationType : s.SuggestedRelationType == relationType)
                            && ((s.Person1Id == p1 && s.Person2Id == p2) || (symmetric && s.Person1Id == p2 && s.Person2Id == p1)));
                        if (pendingAdd)
                        {
                            run.RelationsSkipped++;
                            run.Log.AppendLine($"{tag}: {label} - an unreviewed Add suggestion exists already; run again after reviewing it to attach this evidence");
                            continue;
                        }
                        suggestion.Action = PersonRelationSuggestionAction.Add;
                    }

                    string desc = $"{(suggestion.Action == PersonRelationSuggestionAction.Add ? "ADD" : "ADD-EVIDENCE")} {peopleById[p1].Name} -{(isAffiliation ? affiliationType.ToString() : relationType.ToString())}-> {peopleById[p2].Name} @ {rel.Couplet} «{_AIShort(rel.Quote, 80)}»";
                    if (run.DryRun)
                    {
                        run.Log.AppendLine($"{tag}: would {desc}");
                        continue;
                    }

                    var saved = await run.RelationService.SuggestPersonRelationEditAsync(suggestion, false);
                    if (!string.IsNullOrEmpty(saved.ExceptionString))
                    {
                        run.RelationErrors++;
                        run.Log.AppendLine($"{tag}: {desc} - rejected: {_AIShort(saved.ExceptionString, 250)}");
                        ctx.ChangeTracker.Clear();
                        continue;
                    }
                    if (suggestion.Action == PersonRelationSuggestionAction.Add) run.RelationsCreated++; else run.EvidencesCreated++;
                    run.Log.AppendLine($"{tag}: {desc}");
                }
                catch (Exception exp)
                {
                    run.RelationErrors++;
                    run.Log.AppendLine($"{tag}: relation error {label}: {_AIShort(exp.Message, 250)}");
                    ctx.ChangeTracker.Clear();
                }
            }
        }
    }
}
