using RMuseum.Models.Artifact;
using RMuseum.Models.GanjoorIntegration;
using RMuseum.Models.GanjoorIntegration.ViewModels;
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
        /// import from known sources
        /// </summary>
        /// <param name="srcUrl"></param>
        /// <returns></returns>
        void StartImportingKnownSourceAsync(string srcUrl);
        /// <summary>
        /// get pdf book by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="statusArray"></param>
        /// <param name="includePages"></param>
        /// <param name="includeBookText"></param>
        /// <param name="includePageText"></param>
        /// <returns></returns>
        Task<RServiceResult<PDFBook>> GetPDFBookByIdAsync(int id, PublishStatus[] statusArray, bool includePages, bool includeBookText, bool includePageText);

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
        /// get all books
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, Book[] Books)>> GetAllBooksAsync(PagingParameterModel paging);

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
        Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> GetBookRelatedPDFBooksAsync(PagingParameterModel paging, int bookId);

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

        /// <summary>
        /// get pdf source by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<PDFSource>> GetPDFSourceByIdAsync(int id);

        /// <summary>
        /// Get All PDF Sources
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<PDFSource[]>> GetPDFSourcesAsync();

        /// <summary>
        /// Add PDF Source
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        Task<RServiceResult<PDFSource>> AddPDFSourceAsync(PDFSource source);

        /// <summary>
        /// update PDF Source
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<PDFSource>> UpdatePDFSourceAsync(PDFSource model);

        /// <summary>
        /// delete PDF Source
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeletePDFSourceAsync(int id);

        /// <summary>
        /// get source pdf books
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="sourceId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> GetSourceRelatedPDFBooksAsync(PagingParameterModel paging, int sourceId);

        /// <summary>
        /// batch import soha library
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="finalizeDownload"></param>
        void BatchImportSohaLibraryAsync(int start, int end, bool finalizeDownload);

        /// <summary>
        /// batch import eliteraturebook.com library
        /// </summary>
        /// <param name="ajaxPageIndexStart">from 0</param>
        /// <param name="ajaxPageIndexEnd"></param>
        /// <param name="finalizeDownload"></param>
        void BatchImportELiteratureBookLibraryAsync(int ajaxPageIndexStart, int ajaxPageIndexEnd, bool finalizeDownload);

        /// <summary>
        /// search pdf books
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Items)>> SearchPDFBooksAsync(PagingParameterModel paging, string term);

        /// <summary>
        /// suggest ganjoor link
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="link"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SuggestGanjoorLinkAsync(Guid userId, PDFGanjoorLinkSuggestion link);

        /// <summary>
        /// finds what the method name suggests
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        Task<RServiceResult<GanjoorLinkViewModel>> GetNextUnreviewedGanjoorLinkAsync(int skip);

        /// <summary>
        /// get unreviewed image count
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<int>> GetUnreviewedGanjoorLinksCountAsync();

        /// <summary>
        /// Review Suggested Link
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="userId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> ReviewSuggestedLinkAsync(Guid linkId, Guid userId, ReviewResult result);

        /// <summary>
        /// get unsynced approved pdf ganjoor links
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<PDFGanjoorLink[]>> GetUnsyncedPDFGanjoorLinksAsync();

        /// <summary>
        /// synchronize ganjoor link
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SynchronizePDFGanjoorLinkAsync(Guid linkId);

        /// <summary>
        /// get next un-ocred PDF Book
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<PDFBook>> GetNextUnOCRedPDFBookAsync();

        /// <summary>
        /// reset OCR Queue (remove queued items)
        /// </summary>
        /// <returns></returns>
        Task<RServiceResult<bool>> ResetOCRQueueAsync();

        /// <summary>
        /// set pdf page ocr info (and if a book whole pages are ocred the book ocred flag is set to true)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SetPDFPageOCRInfoAsync(PDFPageOCRDataViewModel model);

        /// <summary>
        /// search pdf books pages for a text
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)>> SearchPDFBookForPDFPagesTextAsync(PagingParameterModel paging, string term);

        /// <summary>
        /// search pdf pages
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="bookId">0 for all pdf books</param>
        /// <param name="term"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, PDFPage[] Items)>> SearchPDFPagesTextAsync(PagingParameterModel paging, int bookId, string term);

        /// <summary>
        /// get page by page number
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        Task<RServiceResult<PDFPage>> GetPDFPageAsync(int pdfBookId, int pageNumber);

        /// <summary>
        /// fill missing book texts
        /// </summary>
        void StartFillingMissingBookTextsAsync();

        /// <summary>
        /// queued downloding pdf books
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, QueuedPDFBook[] Books)>> GetQueuedPDFBooksAsync(PagingParameterModel paging);

        /// <summary>
        /// delete queued books
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteQueuedPDFBookAsync(Guid id);

        /// <summary>
        /// mix queued pdf books 
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> MixQueuedPDFBooksAsync(int step);

        /// <summary>
        /// start processing queue pdf books
        /// </summary>
        /// <param name="count"></param>
        void StartProcessingQueuedPDFBooks(int count);
    }
}
