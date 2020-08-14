using Microsoft.EntityFrameworkCore;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Paginator
    /// </summary>
    public static class QueryablePaginator<ArrayType>
    {
        /// <summary>
        /// paginate
        /// </summary>
        /// <param name="source"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        public static async Task<(PaginationMetadata PagingMeta, ArrayType[] Items)> Paginate(IQueryable<ArrayType> source, PagingParameterModel paging)
        {
            // Get's No of Rows Count   
            int count = source.Count();

            // Parameter is passed from Query string if it is null then it default Value will be pageSize:20  
            int PageSize = paging.PageSize;

            // Parameter is passed from Query string if it is null then it default Value will be pageNumber:1  
            int CurrentPage = paging.PageNumber;

            // Display TotalCount to Records to User  
            int TotalCount = count;

            // Calculating Totalpage by Dividing (No of Records / Pagesize)  
            int TotalPages = PageSize <= 0 ? 1 : (int)Math.Ceiling(count / (double)PageSize);

            // Returns List of Customer after applying Paging   
            var items = PageSize <= 0 ? source.ToList() : source.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

            // if CurrentPage is greater than 1 means it has previousPage  
            bool previousPage = CurrentPage > 1;

            // if TotalPages is greater than CurrentPage means it has nextPage  
            bool nextPage = CurrentPage < TotalPages;

            // Object which we are going to send in header   
            PaginationMetadata paginationMetadata = new PaginationMetadata()
            {
                totalCount = TotalCount,
                pageSize = PageSize,
                currentPage = CurrentPage,
                totalPages = TotalPages,
                hasPreviousPage = previousPage,
                hasNextPage = nextPage
            };

            ArrayType[] result =
                PageSize <= 0 ?
                await source.ToArrayAsync()
                :
                await source.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToArrayAsync();

            return (paginationMetadata, result);

        }
    }
}
