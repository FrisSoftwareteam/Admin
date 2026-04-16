using FirstReg.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FirstReg.Web
{
    public static class Tools
    {
        public static string ReportPath => "wwwroot\\reports";

        // cached reference data to avoid reallocation on each call
        private static readonly List<TeamMember> Directors = new()
        {
            new TeamMember { Name = "Sikiru Rufai", Designation = "Chairman", Photo = "sikiru-rufai.jpg" },
            new TeamMember { Name = "Bayo Olugbemi", Designation = "Managing Director / Chief Executive Officer", Photo = "bayo-olugbemi.jpg" },
            new TeamMember { Name = "Mohammed Balarabe", Designation = "Non Executive Director", Photo = "mohammed-balarabe.jpg" },
            new TeamMember { Name = "Mrs. Foluke Oyeleye", Designation = "Non Executive Director", Photo = "foluke-yeleye.jpg" },
            new TeamMember { Name = "Obafolajimi Otudeko", Designation = "Non Executive Director", Photo = "obafolajimi-otudeko.jpg" }
        };

        private static readonly List<TeamMember> Management = new()
        {
            new TeamMember { Name = "Bayo Olugbemi", Designation = "Managing Director / Chief Executive Officer", Photo = "bayo-olugbemi.jpg" },
            new TeamMember { Name = "Ezekiel Oni", Designation = "Chief Operating Officer (COO)", Photo = "ezekiel-oni.jpg" },
            new TeamMember { Name = "Yaya Lawal", Designation = "Divisional Head, Marketing & Business Development", Photo = "yaya-lawal.jpg" },
            new TeamMember { Name = "Amidu Akinyemi", Designation = "Chief Financial Officer (CFO)", Photo = "amidu-akinyemi.jpg" },
            new TeamMember { Name = "Oluwaseyi Obabunmi Adetola, Esq.", Designation = "Divisional Head, Legal & Regulatory Compliance", Photo = "Oluwaseyi-adetola.jpg" },
            new TeamMember { Name = "Olubunmi Oguntoye", Designation = "Divisional Head, Registrar Services", Photo = "olubunmi-oguntoye.jpg" },
            new TeamMember { Name = "Olukorede Titilolu", Designation = "Divisional Head, Corporate Services", Photo = "olukorede-titilolu.jpg" },
            new TeamMember { Name = "Omowunmi Senkoya", Designation = "Head, Abuja Branch", Photo = "omowunmi-senkoya.jpg" }
        };

        private static readonly List<Video> Media = new()
        {
            new Video("Webinar: Overcoming Bottlenecks in the Probate and Letters of Administration Process in Nigeria", "https://www.youtube.com/live/Y7ZE5z5lP0I?feature=share", "https://img.youtube.com/vi/Y7ZE5z5lP0I/hqdefault.jpg"),
            new Video("Webinar: Evolving Share Data Administration & Registrar Services", "https://www.youtube.com/watch?v=WX3hj-JRFVA", "https://img.youtube.com/vi/WX3hj-JRFVA/hqdefault.jpg"),
            new Video("Navigating Corporate Governance", "https://www.youtube.com/watch?v=jO6RKbGix7c", "https://img.youtube.com/vi/jO6RKbGix7c/hqdefault.jpg"),
            new Video("Understanding Company Structures", "https://www.youtube.com/watch?v=CQoLHnDoeX8", "https://img.youtube.com/vi/CQoLHnDoeX8/hqdefault.jpg"),
            new Video("Overcoming Bottlenecks in the Probate and Letters of Administration Process in Nigeria", "https://www.youtube.com/watch?v=dHzYDa3sfPQ", "https://img.youtube.com/vi/dHzYDa3sfPQ/hqdefault.jpg"),
            new Video("Sec Pigin Language", "https://www.youtube.com/watch?v=HvOYW60zE7k", "t-video.jpg"),
            new Video("Your Shares and the Registrars", "https://www.youtube.com/watch?v=3t7LXvh98xo", "shares-and-reg.jpg"),
            new Video("Sec Pigin Language 2", "https://www.youtube.com/watch?v=xzfAefB6UF4", "s-video.jpg"),
            new Video("First Registrars Mobile App", "https://www.youtube.com/watch?v=G_88U5XnsvE", "f-video.jpg"),
            new Video("CEO, First Registrars Services shares his experience positioning it at the industry’s top player", "https://www.youtube.com/watch?v=wJQDpUu1XYY", "ceo.jpg"),

            //new Video("First Registrars IVR Robocall", "https://www.youtube.com/watch?v=b6CoH03PJ0Y", "robocall.jpg"),
        };

        public static IReadOnlyList<TeamMember> GetDirectors() => Directors;
        public static IReadOnlyList<TeamMember> GetTeams() => Management;
        public static Team GetTeam() => new(Directors, Management);
        public static TeamMember GetMember(string id) =>
            new Team(Directors, Management).List.First(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        public static IReadOnlyList<Video> GetMedia() => Media;
    }

    #region models
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
    public record Team(
        List<TeamMember> Directors,
        List<TeamMember> Management
    )
    {
        public List<TeamMember> List
        {
            get
            {
                List<TeamMember> list = new();
                list.AddRange(Directors);
                list.AddRange(Management);
                return list;
            }
        }
    }
    public class TeamMember
    {
        public string Name { get; set; }
        public string Designation { get; set; }
        public string Photo { get; set; }
        public string Id => Clear.Tools.StringUtility.GenerateUrlKey(Name);

        public string Html { get; set; }
    }
    public record PostPage(
        Post Post,
        List<Post> RecentPosts,
        List<Post> RelatedPosts,
        List<PostCategory> Categories
    );
    public record HomePage(
        List<Post> RecentPosts,
        List<Client> Clients
    );
    public class BlogPost
    {
        public BlogPost(Data.Post post)
        {
            Title = post.Title;
            Brief = post.Brief;
            Content = post.Content;
            Photo = post.Thumb;
            Category = post.Category.Description;
            Author = post.Author.Name;
            Views = post.Views;
            Published = true;
            Promoted = post.Promoted;
            Date = post.Date;
        }

        public string Title { get; set; }
        public string Brief { get; set; }
        public string Content { get; set; }
        public string Photo { get; set; }
        public string Category { get; set; }
        public string Author { get; set; }
        public int Views { get; set; }
        public bool Published { get; set; }
        public bool Promoted { get; set; }
        public DateTime Date { get; set; }
        public string Id => Clear.Tools.StringUtility.GenerateUrlKey(Title);
    }
    public record Client(
        string Name,
        string Logo
    );
    public record Video(
        string Title,
        string Url,
        string Banner
    );
    #endregion
}
