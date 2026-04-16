using FirstReg.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FirstReg.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Service _service;
        readonly string _jsonPath = "wwwroot\\json";
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, Service service, IConfiguration configuration)
        {
            _logger = logger;
            _service = service;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            List<Client> list = new();
            try
            {
                Random rnd = new();

                var path = Path.Combine(Directory.GetCurrentDirectory(), _jsonPath, $"clients.json");
                list = JsonSerializer.Deserialize<List<Client>>(Clear.Tools.FileManager.ReadFile(path)).OrderBy(x => rnd.Next()).ToList();

                return View(new HomePage((await _service.Data.Get<Post>()).OrderByDescending(x => x.Date).Take(12).ToList(), list));
            }
            catch
            {
                return View(new HomePage(new(), list));
            }
        }

        #region about

        [Route("about")]
        public IActionResult About() => View();

        [Route("about/team")]
        public IActionResult Team() => View(Tools.GetTeam());

        [Route("about/team/{id}")]
        public IActionResult TeamMember(string id) => GetTeamMember(id, Tools.GetMember(id));



        [Route("about/directors")]
        public IActionResult Directors() => View(Tools.GetDirectors());

        [Route("about/director/{id}")]
        public IActionResult Director(string id) => GetTeamMember(id, Tools.GetMember(id));

        [Route("about/management-team")]
        public IActionResult ManagementTeam() => View(Tools.GetTeams());

        [Route("about/management-team/{id}")]
        public IActionResult ManagementTeamMember(string id) => GetTeamMember(id, Tools.GetMember(id));

        public IActionResult GetTeamMember(string id, TeamMember p)
        {
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), _jsonPath, $"{id}.json");
                p.Html = Clear.Tools.StringUtility.ParseEditorJS(JsonSerializer.Deserialize<Clear.EditorJS.Content>(Clear.Tools.FileManager.ReadFile(path)));
                return View(p);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return Redirect(Request.Headers[coreTools.UrlReferrer].ToString());
            }
        }

        [Route("about/clients")]
        public IActionResult Clients()
        {
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), _jsonPath, $"clients.json");
                var list = JsonSerializer.Deserialize<List<Client>>(Clear.Tools.FileManager.ReadFile(path));
                return View(list);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return View(new List<Client>());
            }
        }

        #endregion

        #region services

        [Route("/services")]
        public IActionResult Services() => View();

        [Route("/services/register-management")]
        public IActionResult Register() => View();

        [Route("/services/kyc-verification")]
        public IActionResult KYC() => View();

        [Route("/services/electronic-voting")]
        public IActionResult EVoting() => View();

        [Route("/services/probate")]
        public IActionResult Probate() => View();

        #endregion

        #region products

        [Route("/e-products")]
        [Route("/products")]
        public IActionResult Products() => View();

        [Route("/e-products/e-share-notifier")]
        public IActionResult ENotifier() => View();

        [Route("/e-products/online-access")]
        public IActionResult OnlineAccess() => View();

        [Route("/e-products/m-access")]
        public IActionResult MAccess() => View();

        [Route("/e-products/e-lodgement")]
        public IActionResult ELodgement() => View();

        [Route("/e-products/e-dividend")]
        public IActionResult EDividend() => View();

        [Route("/e-products/mobile")]
        public IActionResult Mobile() => View();

        [Route("/e-products/first-dividend-plus-card")]
        public IActionResult DividendCard() => View();

        #endregion

        #region info

        [Route("blog/{cat?}")]
        public async Task<IActionResult> Blog(string cat)
        {
            try
            {
                string sql = $"SELECT Posts.* FROM Posts ";

                if (!string.IsNullOrEmpty(cat))
                    sql += $"INNER JOIN PostCategories ON Posts.CategoryId = PostCategories.Id WHERE PostCategories.Code = '{cat}' ";

                sql += $"WHERE Posts.Type = {(int)PostType.blog} ORDER BY Date DESC";

                return View(await _service.Data.FromSql<Post>(sql));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = "The blog articles could not be listed at this time, please try again";
                return Redirect(Request.Headers[coreTools.UrlReferrer].ToString());
            }
        }

        [Route("post/{id}")]
        public async Task<IActionResult> Post(string id)
        {
            try
            {
                var post = await _service.Data.Get<Post>(x => x.Code == id);
                var posts = await _service.Data.Find<Post>(x => x.Id != post.Id);

                return View(new PostPage(
                    post,
                    posts.OrderByDescending(x => x.Date).Take(5).ToList(),
                    posts.Where(x => x.CategoryId == post.CategoryId).Take(5).ToList(),
                    await _service.Data.Find<PostCategory>(x => x.Type == PostType.blog)
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = "The selected post was not found please try again";
                return Redirect(Request.Headers[coreTools.UrlReferrer].ToString());
            }
        }

        [Route("annual-reports")]
        public async Task<IActionResult> Reports()
        {
            try
            {
                return View(await _service.Data.Get<AnnualReport>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = "The annual reports could not be listed at this time, please try again";
                return Redirect(Request.Headers[coreTools.UrlReferrer].ToString());
            }
        }

        [Route("download-report/{filename}")]
        public async Task<IActionResult> DownloadReports(string filename)
        {
            try
            {
                FileInfo file = new(Path.Combine(Tools.ReportPath, filename));

                if (!file.Exists)
                    throw new InvalidOperationException("the file was not found, please try again later.");

                FileStream fs = new(file.FullName, FileMode.Open, FileAccess.Read);
                BinaryReader br = new(fs);
                long numBytes = file.Length;

                byte[] bytesInStream = br.ReadBytes((int)numBytes); // simpler way of converting to array

                Response.Clear();
                Response.ContentType = "application/force-download";
                Response.Headers.Add("content-disposition", $"attachment;    filename={filename}");
                await Response.BodyWriter.WriteAsync(bytesInStream);
                await Response.CompleteAsync();

                return Ok("done");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = "The annual reports could not be listed at this time, please try again";
                return Redirect(Request.Headers[coreTools.UrlReferrer].ToString());
            }
        }

        [Route("faq")]
        public async Task<IActionResult> FAQ()
        {
            try
            {
                return View(await _service.Data.Get<FaqSection>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                TempData["error"] = "The FAQs could not be listed at this time, please try again";
                return Redirect(Request.Headers[coreTools.UrlReferrer].ToString());
            }
        }

        [Route("media")]
        public IActionResult Media() => View();

        [Route("forms")]
        public IActionResult Forms() => View();

        #endregion

        #region others

        [Route("contact")]
        public IActionResult Contact() => View();

        [Route("privacy")]
        public IActionResult Privacy() => View();

        [Route("whistle-blowing-policy")]
        public IActionResult Whistleblowing() => View();

        [Route("terms")]
        public IActionResult Terms() => View();

        [Route("online-access")]
        public IActionResult OnlineAccessGo() =>
            Redirect(_configuration.GetValue<string>("accessurl"));

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        #endregion

        [Route("subscribe")]
        public async Task<IActionResult> Subscribe(string name, string email)
        {
            try
            {
                if (await _service.Data.ExistsAsync<Contact>(x => x.Email.Trim().ToLower() == email.Trim().ToLower()))
                    return Ok("Subscription already exists");

                await _service.Data.SaveAsync(new Contact { Name = name, Email = email });

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
                return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
            }
        }
    }
}
