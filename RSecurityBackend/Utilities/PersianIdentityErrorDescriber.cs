using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace RSecurityBackend.Utilities
{
    public class PersianIdentityErrorDescriber  : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() => new IdentityError { Code = nameof(DefaultError), Description = $"یک خطای ناشناخته رخ داده است." };
        public override IdentityError ConcurrencyFailure() => new IdentityError { Code = nameof(ConcurrencyFailure), Description = "رکورد جاری پیشتر ویرایش شده‌است و تغییرات شما آن‌را بازنویسی خواهد کرد." };
        public override IdentityError PasswordMismatch() => new IdentityError { Code = nameof(PasswordMismatch), Description = "کلمه عبور نادرست است." };
        public override IdentityError InvalidToken() => new IdentityError { Code = nameof(InvalidToken), Description = "کلمه عبور نامعتبر است." };
        public override IdentityError LoginAlreadyAssociated() => new IdentityError { Code = nameof(LoginAlreadyAssociated), Description = "این کاربر قبلأ اضافه شده‌است." };
        public override IdentityError InvalidUserName(string userName) => new IdentityError { Code = nameof(InvalidUserName), Description = $"نام کاربری '{userName}' نامعتبر است، فقط می تواند حاوی حروف ویا اعداد باشد." };
        public override IdentityError InvalidEmail(string email) => new IdentityError { Code = nameof(InvalidEmail), Description = $"ایمیل '{email}' نامعتبر است." };
        public override IdentityError DuplicateUserName(string userName) => new IdentityError { Code = nameof(DuplicateUserName), Description = $"این نام کاربری '{userName}' به کاربر دیگری اختصاص یافته است." };
        public override IdentityError DuplicateEmail(string email) => new IdentityError { Code = nameof(DuplicateEmail), Description = $"ایمیل '{email}' به کاربر دیگری اختصاص یافته است." };
        public override IdentityError InvalidRoleName(string role) => new IdentityError { Code = nameof(InvalidRoleName), Description = $"نام نقش '{role}' نامعتبر است." };
        public override IdentityError DuplicateRoleName(string role) => new IdentityError { Code = nameof(DuplicateRoleName), Description = $"این نام نقش '{role}' به کاربر دیگری اختصاص یافته است." };
        public override IdentityError UserAlreadyHasPassword() => new IdentityError { Code = nameof(UserAlreadyHasPassword), Description = "کلمه‌ی عبور کاربر قبلأ تنظیم شده‌است." };
        public override IdentityError UserLockoutNotEnabled() => new IdentityError { Code = nameof(UserLockoutNotEnabled), Description = "این کاربر فعال است." };
        public override IdentityError UserAlreadyInRole(string role) => new IdentityError { Code = nameof(UserAlreadyInRole), Description = $"این نقش '{role}' قبلأ به این کاربر اختصاص یافته است." };
        public override IdentityError UserNotInRole(string role) => new IdentityError { Code = nameof(UserNotInRole), Description = $"این نقش '{role}' قبلأ به این کاربر اختصاص نیافته است." };
        public override IdentityError PasswordTooShort(int length) => new IdentityError { Code = nameof(PasswordTooShort), Description = $"کلمه عبور باید حداقل {length} کاراکتر باشد." };
        public override IdentityError PasswordRequiresNonAlphanumeric() => new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "کلمه عبور باید حداقل یک کاراکتر غیر از حروف الفبا داشته باشد." };
        public override IdentityError PasswordRequiresDigit() => new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "کلمه عبور باید حداقل یک عدد داشته باشد." };
        public override IdentityError PasswordRequiresLower() => new IdentityError { Code = nameof(PasswordRequiresLower), Description = "کلمه عبور باید حداقل یک حرف کوچک داشته باشد." };
        public override IdentityError PasswordRequiresUpper() => new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "کلمه عبور باید حداقل یک حرف بزرگ داشته باشد." };
        public override IdentityError RecoveryCodeRedemptionFailed() => new IdentityError { Code = nameof(RecoveryCodeRedemptionFailed), Description = "بازیابی ناموفق بود." };
        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new IdentityError { Code = nameof(PasswordRequiresUniqueChars), Description = $"کلمه عبور باید حداقل داراى {uniqueChars} حرف متفاوت باشد." };
    }
}
