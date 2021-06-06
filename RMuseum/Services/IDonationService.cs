using RMuseum.Models.Accounting;
using RMuseum.Models.Accounting.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// donation service
    /// </summary>
    public interface IDonationService
    {
        /// <summary>
        /// new donation
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="donation"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorDonationViewModel>> AddDonation(Guid editingUserId, GanjoorDonationViewModel donation);

        /// <summary>
        /// delete donation
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteDonation(Guid editingUserId, int id);

        /// <summary>
        /// update donation
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="id"></param>
        /// <param name="updateModel"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UpdateDonation(Guid editingUserId, int id, UpdateDateDescriptionViewModel updateModel);

        /// <summary>
        /// new expense
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="expense"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorExpense>> AddExpense(Guid editingUserId, GanjoorExpense expense);

        /// <summary>
        /// update expense
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="id"></param>
        /// <param name="updateModel"></param>
        /// <returns></returns>

        Task<RServiceResult<bool>> UpdateExpense(Guid editingUserId, int id, UpdateDateDescriptionViewModel updateModel);

        /// <summary>
        /// delete expense
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteExpense(Guid editingUserId, int id);

        /// <summary>
        /// parse html of https://ganjoor.net/donate/ and fill the records
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<bool>> InitializeRecords();

        /// <summary>
        /// regenerate donations page
        /// </summary>
        /// <param name="editingUserId"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RegenerateDonationsPage(Guid editingUserId, string note);

        /// <summary>
        /// returns all donations
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorDonationViewModel[]>> GetDonations();

        /// <summary>
        /// get donation by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorDonationViewModel>> GetDonation(int id);

        /// <summary>
        /// returns all expenses
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<GanjoorExpense[]>> GetExpenses();

        /// <summary>
        /// get expense by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorExpense>> GetExpense(int id);

        /// <summary>
        /// Show Donating Information (temporary switch off/on)
        /// </summary>
        bool ShowAccountInfo { get; }
    }
}
