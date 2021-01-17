using Microsoft.AspNetCore.Identity;

namespace RSecurityBackend.Utilities
{
    /// <summary>
    /// PersianIdentityErrorDescriber 
    /// </summary>
    public class PersianIdentityErrorDescriber  : IdentityErrorDescriber
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError DefaultError() => new IdentityError { Code = nameof(DefaultError), Description = $"یک خطای ناشناخته رخ داده است." };
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError ConcurrencyFailure() => new IdentityError { Code = nameof(ConcurrencyFailure), Description = "رکورد جاری پیشتر ویرایش شده‌است و تغییرات شما آن‌را بازنویسی خواهد کرد." };

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError PasswordMismatch() => new IdentityError { Code = nameof(PasswordMismatch), Description = "کلمه عبور نادرست است." };

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError InvalidToken() => new IdentityError { Code = nameof(InvalidToken), Description = "کلمه عبور نامعتبر است." };

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError LoginAlreadyAssociated() => new IdentityError { Code = nameof(LoginAlreadyAssociated), Description = "این کاربر قبلأ اضافه شده‌است." };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public override IdentityError InvalidUserName(string userName) => new IdentityError { Code = nameof(InvalidUserName), Description = $"نام کاربری '{userName}' نامعتبر است، فقط می تواند حاوی حروف ویا اعداد باشد." };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public override IdentityError InvalidEmail(string email) => new IdentityError { Code = nameof(InvalidEmail), Description = $"ایمیل '{email}' نامعتبر است." };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public override IdentityError DuplicateUserName(string userName) => new IdentityError { Code = nameof(DuplicateUserName), Description = $"این نام کاربری '{userName}' به کاربر دیگری اختصاص یافته است." };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public override IdentityError DuplicateEmail(string email) => new IdentityError { Code = nameof(DuplicateEmail), Description = $"ایمیل '{email}' به کاربر دیگری اختصاص یافته است." };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public override IdentityError InvalidRoleName(string role) => new IdentityError { Code = nameof(InvalidRoleName), Description = $"نام نقش '{role}' نامعتبر است." };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public override IdentityError DuplicateRoleName(string role) => new IdentityError { Code = nameof(DuplicateRoleName), Description = $"این نام نقش '{role}' به کاربر دیگری اختصاص یافته است." };
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError UserAlreadyHasPassword() => new IdentityError { Code = nameof(UserAlreadyHasPassword), Description = "کلمه‌ی عبور کاربر قبلأ تنظیم شده‌است." };
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError UserLockoutNotEnabled() => new IdentityError { Code = nameof(UserLockoutNotEnabled), Description = "این کاربر فعال است." };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public override IdentityError UserAlreadyInRole(string role) => new IdentityError { Code = nameof(UserAlreadyInRole), Description = $"این نقش '{role}' قبلأ به این کاربر اختصاص یافته است." };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public override IdentityError UserNotInRole(string role) => new IdentityError { Code = nameof(UserNotInRole), Description = $"این نقش '{role}' قبلأ به این کاربر اختصاص نیافته است." };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public override IdentityError PasswordTooShort(int length) => new IdentityError { Code = nameof(PasswordTooShort), Description = $"کلمه عبور باید حداقل {length} کاراکتر باشد." };
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError PasswordRequiresNonAlphanumeric() => new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "کلمه عبور باید حداقل یک کاراکتر غیر از حروف الفبا داشته باشد." };
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError PasswordRequiresDigit() => new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "کلمه عبور باید حداقل یک عدد داشته باشد." };
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError PasswordRequiresLower() => new IdentityError { Code = nameof(PasswordRequiresLower), Description = "کلمه عبور باید حداقل یک حرف کوچک داشته باشد." };
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError PasswordRequiresUpper() => new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "کلمه عبور باید حداقل یک حرف بزرگ داشته باشد." };
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IdentityError RecoveryCodeRedemptionFailed() => new IdentityError { Code = nameof(RecoveryCodeRedemptionFailed), Description = "بازیابی ناموفق بود." };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uniqueChars"></param>
        /// <returns></returns>
        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new IdentityError { Code = nameof(PasswordRequiresUniqueChars), Description = $"کلمه عبور باید حداقل داراى {uniqueChars} حرف متفاوت باشد." };
    }
}
