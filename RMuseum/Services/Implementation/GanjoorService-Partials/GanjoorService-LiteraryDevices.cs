using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        private const int LiteraryTextMaxLength = 500;
        private const int LiteraryNoteMaxLength = 1000;
        private const int LiteraryConceptNameMaxLength = 100;

        /// <summary>
        /// normalizes a concept name: Arabic letter forms, extra spaces
        /// </summary>
        private static string _NormalizeConceptName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";
            string n = name.Replace('ي', 'ی').Replace('ك', 'ک').Replace("ۀ", "هٔ");
            n = Regex.Replace(n, @"\s+", " ").Trim();
            return n;
        }

        private static string _TrimOrNull(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        /// <summary>
        /// get literary devices with their slots
        /// </summary>
        /// <param name="status">null: all statuses</param>
        public async Task<RServiceResult<LiteraryDevice[]>> GetLiteraryDevicesAsync(LiteraryEntityStatus? status)
        {
            try
            {
                var query = _context.LiteraryDevices.AsNoTracking().Include(d => d.Slots).AsQueryable();
                if (status != null)
                    query = query.Where(d => d.Status == status);
                var devices = await query.OrderBy(d => d.Category).ThenBy(d => d.Id).ToArrayAsync();
                foreach (var device in devices)
                {
                    device.Slots = device.Slots.OrderBy(s => s.Order).ToList();
                }
                return new RServiceResult<LiteraryDevice[]>(devices);
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryDevice[]>(null, exp.ToString());
            }
        }

        private static string _ValidateDeviceFields(LiteraryDevice device)
        {
            if (string.IsNullOrWhiteSpace(device.Name))
                return "نام آرایه خالی است.";
            if (device.Name.Trim().Length > 100)
                return "نام آرایه بیش از حد طولانی است.";
            if (string.IsNullOrWhiteSpace(device.Definition))
                return "تعریف آرایه لازم است.";
            if (device.Slots == null || device.Slots.Count == 0)
                return "حداقل یک جایگاه برای آرایه لازم است.";
            if (device.Slots.Any(s => string.IsNullOrWhiteSpace(s.Name)))
                return "نام همهٔ جایگاه‌ها لازم است.";
            if (device.Slots.Count > 12)
                return "تعداد جایگاه‌ها بیش از حد است.";
            if (!device.Slots.Any(s => s.Required))
                return "حداقل یک جایگاه باید الزامی باشد.";
            return null;
        }

        private static List<LiteraryDeviceSlot> _CleanSlots(IEnumerable<LiteraryDeviceSlot> slots)
        {
            int order = 0;
            return slots.Select(s => new LiteraryDeviceSlot()
            {
                Key = _TrimOrNull(s.Key),
                Name = s.Name.Trim(),
                Kind = s.Kind,
                Required = s.Required,
                AllowMultiple = s.AllowMultiple,
                Order = order++
            }).ToList();
        }

        /// <summary>
        /// a user suggests a new literary device (it becomes selectable for everyone after a moderator approves it)
        /// </summary>
        public async Task<RServiceResult<LiteraryDevice>> SuggestLiteraryDeviceAsync(Guid userId, LiteraryDevice device)
        {
            try
            {
                string error = _ValidateDeviceFields(device);
                if (error != null)
                    return new RServiceResult<LiteraryDevice>(null, error);

                string name = _NormalizeConceptName(device.Name);
                if (await _context.LiteraryDevices.AsNoTracking().AnyAsync(d => d.Name == name && d.Status != LiteraryEntityStatus.Rejected))
                    return new RServiceResult<LiteraryDevice>(null, "آرایه‌ای با این نام قبلاً ثبت یا پیشنهاد شده است.");

                var dbDevice = new LiteraryDevice()
                {
                    Key = null,
                    Name = name,
                    Category = device.Category,
                    Definition = device.Definition.Trim(),
                    Example = _TrimOrNull(device.Example),
                    Source = _TrimOrNull(device.Source),
                    IsCore = false,
                    Status = LiteraryEntityStatus.Suggested,
                    SuggestedByUserId = userId,
                    CreateDate = DateTime.Now,
                    Slots = _CleanSlots(device.Slots)
                };
                _context.LiteraryDevices.Add(dbDevice);
                await _context.SaveChangesAsync();
                return new RServiceResult<LiteraryDevice>(dbDevice);
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryDevice>(null, exp.ToString());
            }
        }

        /// <summary>
        /// moderate a suggested device: moderated.Status must be Approved or Rejected; the moderator may edit the
        /// fields (and, while the device is still a suggestion, its slots) before approving
        /// </summary>
        public async Task<RServiceResult<LiteraryDevice>> ModerateLiteraryDeviceAsync(Guid moderatorId, LiteraryDevice moderated)
        {
            try
            {
                if (moderated.Status == LiteraryEntityStatus.Suggested)
                    return new RServiceResult<LiteraryDevice>(null, "نتیجهٔ بررسی باید تأیید یا رد باشد.");

                var dbDevice = await _context.LiteraryDevices.Include(d => d.Slots).Where(d => d.Id == moderated.Id).SingleOrDefaultAsync();
                if (dbDevice == null)
                    return new RServiceResult<LiteraryDevice>(null, "آرایه یافت نشد.");

                bool wasSuggestion = dbDevice.Status == LiteraryEntityStatus.Suggested;

                if (moderated.Status == LiteraryEntityStatus.Approved)
                {
                    string error = _ValidateDeviceFields(moderated);
                    if (error != null)
                        return new RServiceResult<LiteraryDevice>(null, error);

                    string name = _NormalizeConceptName(moderated.Name);
                    if (await _context.LiteraryDevices.AsNoTracking().AnyAsync(d => d.Id != dbDevice.Id && d.Name == name && d.Status != LiteraryEntityStatus.Rejected))
                        return new RServiceResult<LiteraryDevice>(null, "آرایه‌ای با این نام وجود دارد.");

                    dbDevice.Name = name;
                    dbDevice.Category = moderated.Category;
                    dbDevice.Definition = moderated.Definition.Trim();
                    dbDevice.Example = _TrimOrNull(moderated.Example);
                    dbDevice.Source = _TrimOrNull(moderated.Source);
                    dbDevice.IsCore = moderated.IsCore;

                    if (wasSuggestion)
                    {
                        //nothing refers to the slots of a device that has not been approved yet
                        _context.LiteraryDeviceSlots.RemoveRange(dbDevice.Slots);
                        dbDevice.Slots = _CleanSlots(moderated.Slots);
                    }
                }
                else if (!wasSuggestion)
                {
                    return new RServiceResult<LiteraryDevice>(null, "امکان رد آرایه‌ای که پیش‌تر تأیید شده نیست.");
                }

                dbDevice.Status = moderated.Status;
                dbDevice.ReviewNote = _TrimOrNull(moderated.ReviewNote);
                await _context.SaveChangesAsync();

                if (wasSuggestion && dbDevice.SuggestedByUserId != null)
                {
                    await _notificationService.PushNotification((Guid)dbDevice.SuggestedByUserId,
                        "بررسی آرایهٔ پیشنهادی شما",
                        dbDevice.Status == LiteraryEntityStatus.Approved
                        ? $"آرایهٔ پیشنهادی شما «{dbDevice.Name}» تأیید شد و اکنون برای برچسب‌گذاری شعرها در دسترس است. با سپاس!"
                        : $"آرایهٔ پیشنهادی شما «{dbDevice.Name}» تأیید نشد.{(string.IsNullOrEmpty(dbDevice.ReviewNote) ? "" : $" توضیح: {dbDevice.ReviewNote}")}"
                        );
                }

                return new RServiceResult<LiteraryDevice>(dbDevice);
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryDevice>(null, exp.ToString());
            }
        }

        private static LiteraryDeviceSlot _Slot(string key, string name, LiteraryDeviceSlotKind kind, bool required, bool multiple = false)
        {
            return new LiteraryDeviceSlot() { Key = key, Name = name, Kind = kind, Required = required, AllowMultiple = multiple };
        }

        /// <summary>
        /// inserts the core devices of the initial list (see the draft list document) if they are not there yet
        /// </summary>
        /// <returns>number of added devices</returns>
        public async Task<RServiceResult<int>> SeedCoreLiteraryDevicesAsync()
        {
            try
            {
                var core = new List<LiteraryDevice>()
                {
                    new LiteraryDevice() { Key = "tashbih", Name = "تشبیه", Category = LiteraryDeviceCategory.Bayan,
                        Definition = "همانندی دو چیز با ذکر دست‌کم مشبّه و مشبّه‌به.",
                        Example = "«رخ تو ماه است»: مشبّه رخ، مشبّه‌به ماه. اگر مشبّه حذف شده باشد استعاره است.",
                        Slots = new List<LiteraryDeviceSlot>() {
                            _Slot("tenor", "مشبّه", LiteraryDeviceSlotKind.SpanConcept, true),
                            _Slot("vehicle", "مشبّه‌به", LiteraryDeviceSlotKind.SpanConcept, true),
                            _Slot("ground", "وجه شبه", LiteraryDeviceSlotKind.Text, false),
                            _Slot("particle", "ادات", LiteraryDeviceSlotKind.Span, false) } },
                    new LiteraryDevice() { Key = "esteare", Name = "استعاره", Category = LiteraryDeviceCategory.Bayan,
                        Definition = "به‌کاربردن واژه برای چیزی جز معنای نخستین، با قرینهٔ مانع از معنای اصلی و علاقهٔ مشابهت.",
                        Example = "«سرو می‌آید» برای قامت یار. اگر علاقه غیر مشابهت باشد مجاز است.",
                        Slots = new List<LiteraryDeviceSlot>() {
                            _Slot("vehicle", "مستعارٌمنه", LiteraryDeviceSlotKind.SpanConcept, true),
                            _Slot("tenor", "مستعارٌله", LiteraryDeviceSlotKind.Concept, true) } },
                    new LiteraryDevice() { Key = "kenaye", Name = "کنایه", Category = LiteraryDeviceCategory.Bayan,
                        Definition = "عبارتی که معنای دوم دارد و معنای حقیقی‌اش هم می‌تواند مراد باشد.",
                        Example = "«دست به دست مالیدن». اگر معنای حقیقی ممکن نباشد استعاره یا مجاز است.",
                        Slots = new List<LiteraryDeviceSlot>() {
                            _Slot("expression", "کنایه‌شونده", LiteraryDeviceSlotKind.Span, true),
                            _Slot("meaning", "معنای دوم", LiteraryDeviceSlotKind.Text, true) } },
                    new LiteraryDevice() { Key = "majaz", Name = "مجاز", Category = LiteraryDeviceCategory.Bayan,
                        Definition = "واژه در معنای غیرحقیقی با علاقه‌ای جز مشابهت و با قرینهٔ مانع.",
                        Example = "«دل» برای عاشق، «جام» برای شراب.",
                        Slots = new List<LiteraryDeviceSlot>() {
                            _Slot("word", "واژه", LiteraryDeviceSlotKind.Span, true),
                            _Slot("meaning", "معنای مرتبط", LiteraryDeviceSlotKind.Concept, true) } },
                    new LiteraryDevice() { Key = "iham", Name = "ایهام", Category = LiteraryDeviceCategory.BadiManavi,
                        Definition = "واژه‌ای با دو معنا که هر دو در بافت سخن پذیرفتنی‌اند.",
                        Example = "اگر یکی از معانی نامحتمل باشد ایهام نیست.",
                        Slots = new List<LiteraryDeviceSlot>() {
                            _Slot("word", "واژه", LiteraryDeviceSlotKind.Span, true),
                            _Slot("meaning", "معنا", LiteraryDeviceSlotKind.Text, true, true) } },
                    new LiteraryDevice() { Key = "jenas", Name = "جناس", Category = LiteraryDeviceCategory.BadiLafzi,
                        Definition = "دو واژه در یک مصراع یا بیت با تلفظ یکسان یا بسیار نزدیک و معنای متفاوت.",
                        Example = "تکرار همان واژه با همان معنا جناس نیست، تکرار است.",
                        Slots = new List<LiteraryDeviceSlot>() {
                            _Slot("first", "واژهٔ اول", LiteraryDeviceSlotKind.Span, true),
                            _Slot("second", "واژهٔ دوم", LiteraryDeviceSlotKind.Span, true) } },
                    new LiteraryDevice() { Key = "tazad", Name = "تضاد", Category = LiteraryDeviceCategory.BadiManavi,
                        Definition = "دو واژهٔ متضاد یا ناسازگار در یک بیت.",
                        Example = "«شب / روز». تناقض (پارادوکس) جدا است.",
                        Slots = new List<LiteraryDeviceSlot>() {
                            _Slot("first", "واژهٔ اول", LiteraryDeviceSlotKind.Span, true),
                            _Slot("second", "واژهٔ دوم", LiteraryDeviceSlotKind.Span, true) } },
                    new LiteraryDevice() { Key = "tekrar", Name = "تکرار", Category = LiteraryDeviceCategory.BadiLafzi,
                        Definition = "تکرار آگاهانهٔ یک واژه یا عبارت با همان معنا.",
                        Example = "ردیف جدا برچسب نمی‌خورد و از ساختار شعر می‌آید.",
                        Slots = new List<LiteraryDeviceSlot>() {
                            _Slot("occurrence", "جای تکرار", LiteraryDeviceSlotKind.Span, true, true) } },
                    new LiteraryDevice() { Key = "eghragh", Name = "اغراق", Category = LiteraryDeviceCategory.BadiManavi,
                        Definition = "بیان چیزی بیش از حدّ امکان یا عادت.",
                        Example = "",
                        Slots = new List<LiteraryDeviceSlot>() {
                            _Slot("expression", "عبارت", LiteraryDeviceSlotKind.Span, true) } },
                    new LiteraryDevice() { Key = "talmih", Name = "تلمیح", Category = LiteraryDeviceCategory.BadiManavi,
                        Definition = "اشاره به داستان، آیه، حدیث، افسانه یا مثل شناخته‌شده.",
                        Example = "",
                        Slots = new List<LiteraryDeviceSlot>() {
                            _Slot("expression", "عبارت", LiteraryDeviceSlotKind.Span, true),
                            _Slot("referent", "موضوع اشاره", LiteraryDeviceSlotKind.Concept, true) } }
                };

                int added = 0;
                foreach (var device in core)
                {
                    if (await _context.LiteraryDevices.AnyAsync(d => d.Key == device.Key))
                        continue;
                    device.IsCore = true;
                    device.Status = LiteraryEntityStatus.Approved;
                    device.CreateDate = DateTime.Now;
                    device.Example = string.IsNullOrEmpty(device.Example) ? null : device.Example;
                    int order = 0;
                    foreach (var slot in device.Slots)
                        slot.Order = order++;
                    _context.LiteraryDevices.Add(device);
                    added++;
                }
                if (added > 0)
                    await _context.SaveChangesAsync();
                return new RServiceResult<int>(added);
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }

        /// <summary>
        /// search approved concepts by name or alias
        /// </summary>
        public async Task<RServiceResult<LiteraryConcept[]>> SearchLiteraryConceptsAsync(string query, int take)
        {
            try
            {
                if (take <= 0 || take > 50)
                    take = 20;
                string q = _NormalizeConceptName(query);
                var concepts = _context.LiteraryConcepts.AsNoTracking().Where(c => c.Status == LiteraryEntityStatus.Approved);
                if (q != "")
                    concepts = concepts.Where(c => c.Name.Contains(q) || (c.Aliases != null && c.Aliases.Contains(q)));
                return new RServiceResult<LiteraryConcept[]>(await concepts.OrderBy(c => c.Name).Take(take).ToArrayAsync());
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryConcept[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// moderator edit of a concept: name, aliases and parent
        /// </summary>
        public async Task<RServiceResult<LiteraryConcept>> UpdateLiteraryConceptAsync(LiteraryConcept concept)
        {
            try
            {
                var db = await _context.LiteraryConcepts.Where(c => c.Id == concept.Id).SingleOrDefaultAsync();
                if (db == null)
                    return new RServiceResult<LiteraryConcept>(null, "مفهوم یافت نشد.");
                string name = _NormalizeConceptName(concept.Name);
                if (name == "" || name.Length > LiteraryConceptNameMaxLength)
                    return new RServiceResult<LiteraryConcept>(null, "نام مفهوم نامعتبر است.");
                if (await _context.LiteraryConcepts.AsNoTracking().AnyAsync(c => c.Id != db.Id && c.Name == name))
                    return new RServiceResult<LiteraryConcept>(null, "مفهومی با این نام وجود دارد؛ برای یکی‌کردن از ادغام استفاده کنید.");
                if (concept.ParentId != null)
                {
                    //no cycles
                    int? p = concept.ParentId;
                    int guard = 0;
                    while (p != null && guard++ < 50)
                    {
                        if (p == db.Id)
                            return new RServiceResult<LiteraryConcept>(null, "مفهوم والد نمی‌تواند خود مفهوم یا زیرمجموعهٔ آن باشد.");
                        p = await _context.LiteraryConcepts.AsNoTracking().Where(c => c.Id == p).Select(c => c.ParentId).SingleOrDefaultAsync();
                    }
                }
                db.Name = name;
                db.Aliases = _TrimOrNull(concept.Aliases);
                db.ParentId = concept.ParentId;
                await _context.SaveChangesAsync();
                return new RServiceResult<LiteraryConcept>(db);
            }
            catch (Exception exp)
            {
                return new RServiceResult<LiteraryConcept>(null, exp.ToString());
            }
        }

        /// <summary>
        /// merge a concept into another one: all tag parts and child concepts move to the target and the
        /// source name becomes an alias of the target
        /// </summary>
        public async Task<RServiceResult<bool>> MergeLiteraryConceptsAsync(int sourceId, int targetId)
        {
            try
            {
                if (sourceId == targetId)
                    return new RServiceResult<bool>(false, "مبدأ و مقصد یکی است.");
                var source = await _context.LiteraryConcepts.Where(c => c.Id == sourceId).SingleOrDefaultAsync();
                var target = await _context.LiteraryConcepts.Where(c => c.Id == targetId).SingleOrDefaultAsync();
                if (source == null || target == null)
                    return new RServiceResult<bool>(false, "مفهوم یافت نشد.");

                //the target must not be a descendant of the source
                int? p = target.ParentId;
                int guard = 0;
                while (p != null && guard++ < 50)
                {
                    if (p == source.Id)
                        target.ParentId = source.ParentId;
                    p = await _context.LiteraryConcepts.AsNoTracking().Where(c => c.Id == p).Select(c => c.ParentId).SingleOrDefaultAsync();
                }

                var parts = await _context.PoemLiteraryTagParts.Where(x => x.ConceptId == sourceId).ToListAsync();
                foreach (var part in parts)
                    part.ConceptId = targetId;
                var children = await _context.LiteraryConcepts.Where(c => c.ParentId == sourceId && c.Id != targetId).ToListAsync();
                foreach (var child in children)
                    child.ParentId = targetId;

                var aliases = new List<string>();
                if (!string.IsNullOrEmpty(target.Aliases))
                    aliases.AddRange(target.Aliases.Split('|').Select(a => a.Trim()).Where(a => a != ""));
                aliases.Add(source.Name);
                if (!string.IsNullOrEmpty(source.Aliases))
                    aliases.AddRange(source.Aliases.Split('|').Select(a => a.Trim()).Where(a => a != ""));
                target.Aliases = string.Join(" | ", aliases.Where(a => a != target.Name).Distinct());

                _context.LiteraryConcepts.Remove(source);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// approved literary tags of a poem (with their parts and concept names)
        /// </summary>
        public async Task<RServiceResult<PoemLiteraryTag[]>> GetPoemLiteraryTagsAsync(int poemId)
        {
            try
            {
                var tags = await _context.PoemLiteraryTags.AsNoTracking()
                    .Include(t => t.Parts).ThenInclude(p => p.Concept)
                    .Where(t => t.PoemId == poemId)
                    .OrderBy(t => t.CoupletIndex).ThenBy(t => t.Id)
                    .ToArrayAsync();
                foreach (var tag in tags)
                    tag.Parts = tag.Parts.OrderBy(p => p.Order).ToList();
                return new RServiceResult<PoemLiteraryTag[]>(tags);
            }
            catch (Exception exp)
            {
                return new RServiceResult<PoemLiteraryTag[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// whether a poem's literary annotation is complete
        /// </summary>
        public async Task<RServiceResult<bool>> GetPoemLiteraryAnnotationCompletedAsync(int poemId)
        {
            try
            {
                return new RServiceResult<bool>(await _context.PoemLiteraryAnnotationStatuses.AsNoTracking().AnyAsync(s => s.PoemId == poemId && s.Completed));
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// delete a tag directly (moderators)
        /// </summary>
        public async Task<RServiceResult<bool>> DeletePoemLiteraryTagAsync(int id)
        {
            try
            {
                var tag = await _context.PoemLiteraryTags.Include(t => t.Parts).Where(t => t.Id == id).SingleOrDefaultAsync();
                if (tag == null)
                    return new RServiceResult<bool>(false, "برچسب یافت نشد.");
                _context.PoemLiteraryTagParts.RemoveRange(tag.Parts);
                _context.PoemLiteraryTags.Remove(tag);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        private static List<LiteraryTagPartSuggestion> _ReadParts(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<LiteraryTagPartSuggestion>();
            return JsonConvert.DeserializeObject<List<LiteraryTagPartSuggestion>>(json) ?? new List<LiteraryTagPartSuggestion>();
        }

        /// <summary>
        /// checks the literary tag suggestions of a poem correction at submission time
        /// </summary>
        /// <returns>an error message, or null if all is fine</returns>
        private async Task<string> _ValidateLiteraryTagCorrectionsAsync(GanjoorPoemLiteraryTagCorrection[] tags)
        {
            if (tags == null)
                return null;
            foreach (var tag in tags)
            {
                tag.SuggestionNote = _TrimOrNull(tag.SuggestionNote);
                if (tag.SuggestionNote != null && tag.SuggestionNote.Length > LiteraryNoteMaxLength)
                    return "توضیح برچسب آرایه بیش از حد طولانی است.";
                if (tag.CoupletIndex < 0)
                    return "شمارهٔ بیت نامعتبر است.";

                if (tag.MarkForDelete)
                {
                    if (tag.ExistingTagId == null || !await _context.PoemLiteraryTags.AsNoTracking().AnyAsync(t => t.Id == tag.ExistingTagId))
                        return "برچسب مورد نظر برای حذف وجود ندارد.";
                    continue;
                }

                var device = await _context.LiteraryDevices.AsNoTracking().Include(d => d.Slots)
                    .Where(d => d.Id == tag.DeviceId && d.Status == LiteraryEntityStatus.Approved).SingleOrDefaultAsync();
                if (device == null)
                    return "آرایهٔ انتخاب‌شده وجود ندارد یا هنوز تأیید نشده است.";

                List<LiteraryTagPartSuggestion> parts;
                try
                {
                    parts = _ReadParts(tag.PartsJson);
                }
                catch (Exception)
                {
                    return $"مقادیر جایگاه‌های «{device.Name}» قابل خواندن نیست.";
                }

                foreach (var slot in device.Slots)
                {
                    var slotParts = parts.Where(p => p.SlotId == slot.Id).ToList();
                    if (slot.Required && slotParts.Count == 0)
                        return $"جایگاه «{slot.Name}» در آرایهٔ «{device.Name}» الزامی است.";
                    if (!slot.AllowMultiple && slotParts.Count > 1)
                        return $"جایگاه «{slot.Name}» در آرایهٔ «{device.Name}» فقط یک بار می‌تواند پر شود.";
                }

                foreach (var part in parts)
                {
                    var slot = device.Slots.FirstOrDefault(s => s.Id == part.SlotId);
                    if (slot == null)
                        return $"جایگاهی که برای آرایهٔ «{device.Name}» ارسال شده متعلق به آن نیست.";

                    bool needsSpan = slot.Kind == LiteraryDeviceSlotKind.Span || slot.Kind == LiteraryDeviceSlotKind.SpanConcept;
                    bool needsConcept = slot.Kind == LiteraryDeviceSlotKind.Concept || slot.Kind == LiteraryDeviceSlotKind.SpanConcept;
                    bool needsText = slot.Kind == LiteraryDeviceSlotKind.Text;

                    if (needsSpan)
                    {
                        if (part.CoupletIndex < 1 || part.Misra < 1 || part.Misra > 2 || part.WordStart == null || part.WordEnd == null
                            || part.WordStart < 1 || part.WordEnd < part.WordStart)
                            return $"جای «{slot.Name}» در متن شعر مشخص نشده است.";
                    }
                    else if (part.CoupletIndex < 0 || part.Misra < 0 || part.Misra > 2)
                    {
                        return $"جای «{slot.Name}» نامعتبر است.";
                    }

                    if (needsConcept)
                    {
                        if (part.ConceptId == null && string.IsNullOrWhiteSpace(part.SuggestedConceptName))
                            return $"مفهوم «{slot.Name}» مشخص نشده است.";
                        if (part.ConceptId != null && !await _context.LiteraryConcepts.AsNoTracking().AnyAsync(c => c.Id == part.ConceptId && c.Status == LiteraryEntityStatus.Approved))
                            return $"مفهوم انتخاب‌شده برای «{slot.Name}» وجود ندارد.";
                        if (part.ConceptId == null)
                        {
                            part.SuggestedConceptName = _NormalizeConceptName(part.SuggestedConceptName);
                            if (part.SuggestedConceptName.Length > LiteraryConceptNameMaxLength)
                                return $"نام مفهوم «{slot.Name}» بیش از حد طولانی است.";
                        }
                    }
                    if (needsText)
                    {
                        if (string.IsNullOrWhiteSpace(part.Text))
                            return $"متن «{slot.Name}» خالی است.";
                        if (part.Text.Length > LiteraryTextMaxLength)
                            return $"متن «{slot.Name}» بیش از حد طولانی است.";
                    }
                }

                //normalized, cleaned parts go back to the stored json
                tag.PartsJson = JsonConvert.SerializeObject(parts);
            }
            return null;
        }

        private static string _PartsSignature(IEnumerable<Tuple<int, int, int, int?, int?, int?, string>> parts)
        {
            return string.Join(";", parts.OrderBy(p => p.Item1).ThenBy(p => p.Item2).ThenBy(p => p.Item3).ThenBy(p => p.Item4)
                .Select(p => $"{p.Item1}|{p.Item2}|{p.Item3}|{p.Item4}|{p.Item5}|{p.Item6}|{p.Item7}"));
        }

        /// <summary>
        /// applies an approved literary tag suggestion to the live data (called by ModeratePoemCorrection)
        /// </summary>
        /// <returns>an error message, or null if applied</returns>
        private async Task<string> _ApplyApprovedLiteraryTagAsync(GanjoorPoemCorrection dbCorrection, GanjoorPoemLiteraryTagCorrection suggestion)
        {
            if (suggestion.MarkForDelete)
            {
                var existing = await _context.PoemLiteraryTags.Include(t => t.Parts).Where(t => t.Id == suggestion.ExistingTagId).SingleOrDefaultAsync();
                if (existing == null)
                    return "برچسب آرایهٔ مورد نظر برای حذف دیگر وجود ندارد.";

                //snapshot onto the correction row itself so the deletion can be understood (and undone) later
                suggestion.DeviceId = existing.DeviceId;
                suggestion.CoupletIndex = existing.CoupletIndex;
                suggestion.Uncertain = existing.Uncertain;
                suggestion.PartsJson = JsonConvert.SerializeObject(existing.Parts.Select(p => new LiteraryTagPartSuggestion()
                {
                    SlotId = p.SlotId,
                    CoupletIndex = p.CoupletIndex,
                    Misra = p.Misra,
                    WordStart = p.WordStart,
                    WordEnd = p.WordEnd,
                    ConceptId = p.ConceptId,
                    Text = p.Text
                }).ToList());

                _context.PoemLiteraryTagParts.RemoveRange(existing.Parts);
                _context.PoemLiteraryTags.Remove(existing);
                return null;
            }

            var device = await _context.LiteraryDevices.AsNoTracking().Include(d => d.Slots)
                .Where(d => d.Id == suggestion.DeviceId && d.Status == LiteraryEntityStatus.Approved).SingleOrDefaultAsync();
            if (device == null)
                return "آرایهٔ مربوط به این برچسب دیگر در دسترس نیست.";

            List<LiteraryTagPartSuggestion> parts;
            try
            {
                parts = _ReadParts(suggestion.PartsJson);
            }
            catch (Exception)
            {
                return "مقادیر جایگاه‌های برچسب آرایه قابل خواندن نیست.";
            }

            //resolve concepts: reuse an approved concept with the same normalized name/alias, else create it
            foreach (var part in parts)
            {
                if (part.ConceptId != null || string.IsNullOrWhiteSpace(part.SuggestedConceptName))
                    continue;
                string name = _NormalizeConceptName(part.SuggestedConceptName);
                var sameName = await _context.LiteraryConcepts.Where(c => c.Name == name || (c.Aliases != null && c.Aliases.Contains(name))).ToListAsync();
                var found = sameName.FirstOrDefault(c => c.Name == name)
                    ?? sameName.FirstOrDefault(c => c.Aliases.Split('|').Select(a => a.Trim()).Contains(name));
                if (found == null)
                {
                    found = new LiteraryConcept()
                    {
                        Name = name,
                        Status = LiteraryEntityStatus.Approved,
                        CreatedByUserId = dbCorrection.UserId,
                        CreateDate = DateTime.Now
                    };
                    _context.LiteraryConcepts.Add(found);
                    await _context.SaveChangesAsync();
                }
                part.ConceptId = found.Id;
                part.SuggestedConceptName = null;
            }

            //the same thing must not be tagged twice
            string signature = _PartsSignature(parts.Select(p => Tuple.Create(p.SlotId, p.CoupletIndex, p.Misra, p.WordStart, p.WordEnd, p.ConceptId, _TrimOrNull(p.Text))));
            var sameDevice = await _context.PoemLiteraryTags.AsNoTracking().Include(t => t.Parts)
                .Where(t => t.PoemId == dbCorrection.PoemId && t.CoupletIndex == suggestion.CoupletIndex && t.DeviceId == suggestion.DeviceId).ToListAsync();
            foreach (var other in sameDevice)
            {
                if (_PartsSignature(other.Parts.Select(p => Tuple.Create(p.SlotId, p.CoupletIndex, p.Misra, p.WordStart, p.WordEnd, p.ConceptId, _TrimOrNull(p.Text)))) == signature)
                    return $"برچسب آرایهٔ «{device.Name}» با همین مشخصات قبلاً ثبت شده است.";
            }

            int order = 0;
            var newTag = new PoemLiteraryTag()
            {
                PoemId = dbCorrection.PoemId,
                CoupletIndex = suggestion.CoupletIndex,
                DeviceId = suggestion.DeviceId,
                Uncertain = suggestion.Uncertain,
                Note = suggestion.SuggestionNote,
                Origin = LiteraryTagOrigin.Editor,
                CreatedByUserId = dbCorrection.UserId,
                CreateDate = DateTime.Now,
                Parts = parts.Select(p => new PoemLiteraryTagPart()
                {
                    SlotId = p.SlotId,
                    CoupletIndex = p.CoupletIndex,
                    Misra = p.Misra,
                    WordStart = p.WordStart,
                    WordEnd = p.WordEnd,
                    ConceptId = p.ConceptId,
                    Text = _TrimOrNull(p.Text),
                    Order = order++
                }).ToList()
            };
            _context.PoemLiteraryTags.Add(newTag);
            await _context.SaveChangesAsync();

            //record which live tag this correction produced (same trick as the geo tags)
            suggestion.ExistingTagId = newTag.Id;
            suggestion.PartsJson = JsonConvert.SerializeObject(parts);
            return null;
        }

        private async Task _ApplyLiteraryAnnotationStatusAsync(int poemId, bool completed, Guid userId)
        {
            var status = await _context.PoemLiteraryAnnotationStatuses.Where(s => s.PoemId == poemId).SingleOrDefaultAsync();
            if (status == null)
            {
                status = new PoemLiteraryAnnotationStatus() { PoemId = poemId };
                _context.PoemLiteraryAnnotationStatuses.Add(status);
            }
            status.Completed = completed;
            status.UserId = userId;
            status.Date = DateTime.Now;
        }
    }
}
