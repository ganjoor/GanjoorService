using RMuseum.Models.Artifact;
using RMuseum.Models.PDFLibrary;
using RMuseum.Models.PDFLibrary.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// PDF Library Services
    /// </summary>
    public interface IPDFLibraryService
    {
        /// <summary>
        /// get pdf book by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        Task<RServiceResult<PDFBook>> GetPDFBookByIdAsync(int id, PublishStatus[] statusArray);

        /// <summary>
        /// get all pdfbooks (including CoverImage info but not pages or tagibutes info)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> GetAllPDFBooksAsync(PagingParameterModel paging, PublishStatus[] statusArray);

        /// <summary>
        /// an incomplete prototype for removing PDF books
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> RemovePDFBookAsync(int pdfBookId);

        /// <summary>
        /// add pdf book tag value
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="rTag"></param>
        /// <returns></returns>
        Task<RServiceResult<RTagValue>> TagPDFBookAsync(int pdfBookId, RTag rTag);

        /// <summary>
        /// remove pdf book tag value
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> UnTagPDFBookAsync(int pdfBookId, Guid tagValueId);

        /// <summary>
        /// edit pdf book tag value
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="edited"></param>
        /// <param name="global">apply on all same value tags</param>
        /// <returns></returns>
        Task<RServiceResult<RTagValue>> EditPDFBookTagValueAsync(int pdfBookId, RTagValue edited, bool global);

        /// <summary>
        /// get tagged publish pdfbooks (including CoverImage info but not pages or tagibutes info) 
        /// </summary>
        /// <param name="tagUrl"></param>
        /// <param name="valueUrl"></param>
        /// <param name="statusArray"></param>
        /// <returns></returns>
        Task<RServiceResult<PDFBook[]>> GetPDFBookByTagValueAsync(string tagUrl, string valueUrl, PublishStatus[] statusArray);

        /// <summary>
        /// add author
        /// </summary>
        /// <param name="author"></param>
        /// <returns></returns>
        Task<RServiceResult<Author>> AddAuthorAsync(Author author);

        /// <summary>
        /// get author by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<Author>> GetAuthorByIdAsync(int id);

        /// <summary>
        /// get authors
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="authorName"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, Author[] Authors)>> GetAuthorsAsync(PagingParameterModel paging, string authorName);

        /// <summary>
        /// update author
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<Author>> UpdateAuthorAsync(Author model);

        /// <summary>
        /// delete author by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteAuthorAsync(int id);

        /// <summary>
        /// add pdf book contributer
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="authorId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AddPDFBookContributerAsync(int pdfBookId, int authorId, string role);

        /// <summary>
        /// remove contribution from pdf book
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="contributionId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePDFBookContributerAsync(int pdfBookId, int contributionId);

        /// <summary>
        /// get published pdf books by author
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="authorId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> GetPublishedPDFBooksByAuthorAsync(PagingParameterModel paging, int authorId, string role);

        /// <summary>
        /// get published pdf books by author stats (group by role)
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>

        Task<RServiceResult<AuthorRoleCount[]>> GetPublishedPDFBookbyAuthorGroupedByRoleAsync(int authorId);

        /// <summary>
        /// add book
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        Task<RServiceResult<Book>> AddBookAsync(Book book);

        /// <summary>
        /// update book
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<Book>> UpdateBookAsync(Book model);

        /// <summary>
        /// delete book
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteBookAsync(int id);

        /// <summary>
        /// add book author
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="authorId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> AddBookAuthorAsync(int bookId, int authorId, string role);

        /// <summary>
        /// remove author from book
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="contributionId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteBookAuthorAsync(int bookId, int contributionId);

        /// <summary>
        /// book by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<Book>> GetBookByIdAsync(int id);

        /// <summary>
        /// get books by author
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="authorId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, Book[] Books)>> GetBooksByAuthorAsync(PagingParameterModel paging, int authorId, string role);

        /// <summary>
        /// get books by author stats (group by role)
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>

        Task<RServiceResult<AuthorRoleCount[]>> GetBookbyAuthorGroupedByRoleAsync(int authorId);

        /// <summary>
        /// get book related pdf books
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="bookId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> GetBookRelatedPFFBooksAsync(PagingParameterModel paging, int bookId);

        /// <summary>
        /// add multi volume pdf collection
        /// </summary>
        /// <param name="multiVolumePDFCollection"></param>
        /// <returns></returns>
        Task<RServiceResult<MultiVolumePDFCollection>> AddMultiVolumePDFCollectionAsync(MultiVolumePDFCollection multiVolumePDFCollection);

        /// <summary>
        /// update multi volume pdf collection
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<MultiVolumePDFCollection>> UpdateMultiVolumePDFCollectionAsync(MultiVolumePDFCollection model);

        /// <summary>
        /// delete multi volume pdf collection
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteMultiVolumePDFCollectionAsync(int id);


        /// <summary>
        /// start importing local pdf file
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> StartImportingLocalPDFAsync(NewPDFBookViewModel model);

        /// <summary>
        /// edit pdf book master record
        /// </summary>
        /// <param name="model"></param>
        /// <param name="canChangeStatusToAwaiting"></param>
        /// <param name="canPublish"></param>
        /// <returns></returns>
        Task<RServiceResult<PDFBook>> EditPDFBookMasterRecordAsync(PDFBook model, bool canChangeStatusToAwaiting, bool canPublish);

        /// <summary>
        /// Copy PDF Book Cover Image From Page Thumbnail image
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="pdfpageId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SetPDFBookCoverImageFromPageAsync(int pdfBookId, int pdfpageId);

        /// <summary>
        /// get volumes pdf books
        /// </summary>
        /// <param name="volumeId"></param>
        /// <returns></returns>
        Task<RServiceResult<PDFBook[]>> GetVolumesPDFBooks(int volumeId);

        /// <summary>
        /// get volumes by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<MultiVolumePDFCollection>> GetMultiVolumePDFCollectionByIdAsync(int id);
    }
}
