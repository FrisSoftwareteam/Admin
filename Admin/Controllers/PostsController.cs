using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.Admin.Controllers
{
    [Authorize]
    [Route("content")]
    public class PostsController(ILogger<PostsController> logger, Service service) : Controller
    {

        #region posts

        [HttpGet("{type}")]
        public async Task<IActionResult> Index(PostType type) =>
            View(await service.Data.Find<Post>(x => x.Type == type));

        [HttpGet("details/{code}")]
        public async Task<IActionResult> Details(string code)
        {
            try
            {
                var posts = await service.Data.Find<Post>(x => x.Code.ToLower() == code.ToLower());
                if (posts.Count > 0) return View(posts.First());
                return NotFound("The post does not exist");
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
                return Redirect(Request.Headers["Referrer"].ToString());
            }
        }

        [HttpGet("{type}/update/{code?}")]
        public async Task<IActionResult> Update(PostType type, string code)
        {
            var cats = await service.Data.Get<PostCategory>();
            var authors = await service.Data.Get<Author>();

            try
            {
                if (!string.IsNullOrEmpty(code))
                    return View(new PostPageModel(await service.Data.Get<Post>(x => x.Code.ToLower() == code.ToLower()), cats, authors));

                return View(new PostPageModel(type, cats, authors));
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
                return Redirect(Request.Headers["Referrer"].ToString());
            }
        }

        [HttpPost("{type}/update/{code?}")]
        public async Task<IActionResult> Update(PostType type, PostPageModel model, IFormFile mfile, string code)
        {
            try
            {
                ImageSize imgSize = new(720, 405);

                code = Clear.Tools.StringUtility.GenerateUrlKey(model.Title);

                if (model.Id > 0) // update
                {
                    var post = await service.Data.Get<Post>(x => x.Id == model.Id);

                    post.Title = model.Title;
                    post.Brief = model.Brief;
                    post.CategoryId = model.CategoryId;
                    post.Html = model.Html;
                    post.AuthorId = model.AuthorId;
                    post.Promoted = model.Promoted;

                    if (mfile != null && mfile.Length > 0)
                    {
                        var oldFilename = post.Thumb;

                        post.Thumb = Tools.GenerateFileName(model.Title, mfile.FileName.Split('.').Last());
                        await Tools.UploadFileAsync(mfile, post.Thumb, blobfolder.posts, imgSize);

                        Clear.Tools.FileManager.DeleteFromAzure(
                            Tools.BlobConnectionString, Tools.BlobContainerName, oldFilename,
                            Tools.GetUploadPath(blobfolder.posts));
                    }

                    await service.Data.UpdateAsync(post);
                }
                else
                {
                    if (mfile == null || mfile.Length <= 0)
                        throw new ArgumentNullException("Please upload an image for this post");

                    model.Thumb = Tools.GenerateFileName(model.Title, mfile.FileName.Split('.').Last());
                    await Tools.UploadFileAsync(mfile, model.Thumb, blobfolder.posts, imgSize);

                    model.Code = code;
                    model.Date = DateTime.Now;

                    await service.Data.SaveAsync<Post>(model);
                }

                TempData["success"] = "Successful";
                return RedirectToAction(nameof(Details), new { code });
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
                logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return View(new PostPageModel(model, await service.Data.Get<PostCategory>(), await service.Data.Get<Author>()));
            }
        }

        [Route("delete/{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    throw new InvalidOperationException("You have not selected any post to be deleted");

                var posts = await service.Data.Find<Post>(x => x.Code.ToLower() == code.ToLower());

                if (posts.Count <= 0)
                    return NotFound("The post does not exist");

                var post = posts.First();

                await service.Data.DeleteAsync(post);

                Clear.Tools.FileManager.DeleteFromAzure(
                    Tools.BlobConnectionString, Tools.BlobContainerName, post.Thumb,
                    Tools.GetUploadPath(blobfolder.posts));

                TempData["success"] = "The post was successfully deleted";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }

            return Redirect(Request.Headers["Referrer"].ToString());
        }

        #endregion

        #region categories

        [Route("categories")]
        public async Task<IActionResult> Categories() => View(await service.Data.Get<PostCategory>());

        [HttpPost("categories/update")]
        public async Task<IActionResult> UpdateCategory(PostCategory model)
        {
            try
            {
                if (model.Id > 0)
                    await service.Data.UpdateAsync(model);
                else
                {
                    model.Type = PostType.blog;
                    model.Code = Clear.Tools.StringUtility.GenerateUrlKey(model.Description);
                    await service.Data.SaveAsync(model);
                }

                TempData["success"] = "Successful";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }
            return Redirect(Request.Headers["Referrer"].ToString());
        }

        [Route("categories/delete/{code}")]
        public async Task<IActionResult> DeleteCategory(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    throw new InvalidOperationException("You have not selected any category to be deleted");

                var categories = await service.Data.Find<PostCategory>(x => x.Code.ToLower() == code.ToLower());

                if (categories.Count <= 0)
                    return NotFound("The category does not exist");

                var category = categories.First();

                await service.Data.DeleteAsync(category);

                TempData["success"] = "The category was deleted";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }

            return Redirect(Request.Headers["Referrer"].ToString());
        }

        #endregion

        #region authors

        [HttpGet("authors")]
        public async Task<IActionResult> Authors() => View(await service.Data.Get<Author>());

        [HttpPost("authors/update")]
        public async Task<IActionResult> UpdateAuthor(Author model, IFormFile mfile)
        {
            try
            {
                ImageSize imgSize = new(300, 300);

                string code = Clear.Tools.StringUtility.GenerateUrlKey(model.Name);

                if (model.Id > 0) // update
                {
                    var post = await service.Data.Get<Author>(x => x.Id == model.Id);

                    post.Name = model.Name;
                    post.Bio = model.Bio;

                    if (mfile != null && mfile.Length > 0)
                    {
                        var oldFilename = post.Avatar;

                        post.Avatar = Tools.GenerateFileName(model.Name, mfile.FileName.Split('.').Last());
                        await Tools.UploadFileAsync(mfile, post.Avatar, blobfolder.posts, imgSize);

                        Clear.Tools.FileManager.DeleteFromAzure(
                            Tools.BlobConnectionString, Tools.BlobContainerName, oldFilename,
                            Tools.GetUploadPath(blobfolder.posts));
                    }

                    await service.Data.UpdateAsync(post);
                }
                else
                {
                    if (mfile == null || mfile.Length <= 0)
                        throw new ArgumentNullException("Please upload an image for this post");

                    model.Avatar = Tools.GenerateFileName(model.Name, mfile.FileName.Split('.').Last());
                    await Tools.UploadFileAsync(mfile, model.Avatar, blobfolder.posts, imgSize);

                    //model.Code = code;
                    //model.Date = DateTime.Now;

                    await service.Data.SaveAsync<Author>(model);
                }

                TempData["success"] = "Successful";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
                return View(model);
            }

            return Redirect(Request.Headers["Referrer"].ToString());
        }

        [Route("authors/delete/{id}")]
        public async Task<IActionResult> DeleteAuthor(int id)
        {
            try
            {
                if (id == 0)
                    throw new InvalidOperationException("You have not selected any author to be deleted");

                var authors = await service.Data.Find<Author>(x => x.Id == id);

                if (authors.Count <= 0)
                    return NotFound("The author does not exist");

                var author = authors.First();

                await service.Data.DeleteAsync(author);

                TempData["success"] = "Successful";
            }
            catch (Exception ex)
            {
                TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
            }

            return Redirect(Request.Headers["Referrer"].ToString());
        }

        #endregion
    }
}