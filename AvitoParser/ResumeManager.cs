using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace AvitoParser
{
    public static class ResumeManager
    {
        static Type ThisType = typeof(ResumeManager);
        static string DescriptionXPath = "//div[@class='item-description-text']/p";
        static string AddressXPath = "//div[@class='seller-info-prop']";
        static string PositionXPath = "//span[@class='title-info-title-text']";
        static string CitizenshipXPath = "//p[@class='resume-params-text']";
        static string UlXPath = "//div[@class='item-params item-params_type-one-colon']/ul/li";
        static string LiXPath = "//span[@class='item-params-label']";
        static string DateXPath = "//div[@class='title-info-metadata-item']";
        static string UrlsXPath = "//a[@class='item-description-title-link']";
        static string PriceFlag = "avito.item.price = '";
        static string IdFlag = "avito.item.id = '";


        

        public static List<Candidate> GetCandidates(string url)
        {
            HashSet<string> urls = new HashSet<string>();
            List<Candidate> candidates = new List<Candidate>();

            if (!String.IsNullOrEmpty(url))
            {
                using (var webClient = new WebClient())
                {
                    try
                    {
                        var html = webClient.DownloadString(url);
                        var contentType = webClient.ResponseHeaders["content-type"];

                        if (!string.IsNullOrWhiteSpace(contentType) && contentType.Contains("text/html"))
                        {
                            Log.For(ThisType).Info("Start searching for resume urls. Base url: " + url);
                            urls = GetUrls(url);
                            Log.For(ThisType).Info("Finish searching for resume urls. \nStart getting resumes.");

                            foreach (string u in urls)
                            {
                                candidates.Add(GetCandidateFromHtml(u));
                            }

                            Log.For(ThisType).Info("Finish getting resumes.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.For(ThisType).Error(ex);
                    }
                }

            }

            return candidates;
        }



        enum Feature
        {
            Description,
            Address,
            Position,
            Salary,
            Id,
            Citizenship
        };



        private static Candidate GetCandidateFromHtml(string url)
        {
            Candidate candidate = new Candidate();
            IEnumerable<string> nodes;
            HtmlNode node;
            string key = string.Empty;
            string value = string.Empty;
            HtmlWeb web = new HtmlWeb();
            HtmlDocument partialDoc;
            int intValue = 0;
            bool isReady = false;

            HtmlDocument doc = web.Load(url);

            candidate.Url = url;
            candidate.CreatingDate = GetRegistrationDate(doc);
            candidate.AvitoId = GetInnerIntFromScripts(doc, Feature.Id);
            candidate.Salary = GetInnerIntFromScripts(doc, Feature.Salary);
            candidate.Description = GetInnerTextFromTags(doc, Feature.Description);
            candidate.Address = GetInnerTextFromTags(doc, Feature.Address);
            candidate.Position = GetInnerTextFromTags(doc, Feature.Position);
            candidate.Citizenship = GetInnerTextFromTags(doc, Feature.Citizenship);

            nodes = doc.DocumentNode
                .SelectNodes(UlXPath)
                .Select(li => li.OuterHtml);

            foreach (string n in nodes)
            {
                partialDoc = new HtmlDocument();
                partialDoc.LoadHtml(n);
                node = partialDoc.DocumentNode.SelectSingleNode(LiXPath);
                key = GetCorrectString(node.InnerText, false);
                value = GetCorrectString(node.NextSibling.InnerText, false);

                switch (key)
                {
                    case "Образование":
                        {
                            candidate.Education = value;
                            break;
                        }
                    case "Опыт работы":
                        {
                            if (Int32.TryParse(value, out intValue))
                            {
                                candidate.Experience = intValue;
                            }
                            break;
                        }
                    case "Пол":
                        {
                            candidate.Sex = value;
                            break;
                        }
                    case "Возраст":
                        {
                            Int32.TryParse(value, out intValue);
                            candidate.Age = intValue;
                            break;
                        }
                    case "Готовность к командировкам":
                        {
                            candidate.BusinessTripReady = value;
                            break;
                        }
                    case "Переезд":
                        {
                            if (Boolean.TryParse(value, out isReady))
                                candidate.RemovalReady = isReady;
                            else
                                candidate.RemovalReady = !isReady;
                            break;
                        }
                    case "Сфера деятельности":
                        {
                            candidate.ActionSphere = value;
                            break;
                        }
                    case "График работы":
                        {
                            candidate.WorkingSchedule = value;
                            break;
                        }
                }
            }
            return candidate;
        }

        private static int GetInnerIntFromScripts(HtmlDocument doc, Feature feature)
        {
            int index;
            int result = 0;
            string flag = String.Empty;
            string tempStr = String.Empty;

            switch (feature)
            {
                case Feature.Salary:
                    flag = PriceFlag;
                    break;
                case Feature.Id:
                    flag = IdFlag;
                    break;
            }

            tempStr = doc.DocumentNode.Descendants()
                            .Where(n => n.Name == "script" && n.InnerText.Contains(flag))
                            .First().InnerText;

            tempStr = tempStr.Substring(tempStr.LastIndexOf(flag) + flag.Length);

            index = tempStr.IndexOf("'");

            if (index > 0)
                tempStr = tempStr.Substring(0, index);

            Int32.TryParse(tempStr, out result);

            return result;
        }


        private static string GetInnerTextFromTags(HtmlDocument doc, Feature feature)
        {
            string result = String.Empty;
            string xPath = String.Empty;
            HtmlNodeCollection nodes;
            bool ignoreDigits = false;

            switch (feature)
            {
                case Feature.Description:
                    xPath = DescriptionXPath;
                    ignoreDigits = true;
                    break;
                case Feature.Address:
                    xPath = AddressXPath;
                    ignoreDigits = false;
                    break;
                case Feature.Position:
                    xPath = PositionXPath;
                    ignoreDigits = false;
                    break;
                case Feature.Citizenship:
                    xPath = CitizenshipXPath;
                    ignoreDigits = true;
                    break;
            }

            nodes = doc.DocumentNode.LastChild.SelectNodes(xPath);

            if (feature == Feature.Citizenship)
            {
                foreach (var node in nodes)
                {
                    if (node.InnerText.Contains("Гражданство"))
                    {
                        result = GetCorrectString(node.InnerText, ignoreDigits);
                        result = result.Substring(result.IndexOf(";") + 1);
                        result = result.Trim();
                    }
                }
            }
            else if (feature == Feature.Address)
            {
                string adr = "Адрес";

                foreach (var node in nodes)
                {
                    if (node.InnerText.Contains(adr))
                    {
                        result = GetCorrectString(node.InnerText, ignoreDigits);
                        result = result.Substring(result.IndexOf(adr) + adr.Length);
                        result = result.Trim();
                        break;
                    }
                }
            }
            else
            {
                result = GetCorrectString(nodes[0].InnerText, ignoreDigits);
            }

            return result;
        }


        private static string GetCorrectString(string str, bool ignoreDigits)
        {
            string result = String.Empty;
            string strDigits = String.Empty;
            StringBuilder builder;

            result = str.Trim();

            if (result.EndsWith(":"))
            {
                result = result.Remove(result.Length - 1);
            }
            if (result.EndsWith("\n"))
            {
                result = result.Remove(result.Length - 2);
            }
            if (result.Contains("&nbsp;"))
            {
                builder = new StringBuilder(result);
                builder.Replace("&nbsp;", " ");
                result = builder.ToString();
            }

            if (ignoreDigits)
                return result;


            if (result.Any(c => char.IsDigit(c)) && (result.Contains("год") || result.Contains("лет")))
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if (char.IsDigit(str[i]))
                        strDigits = strDigits + str[i];
                }

                return strDigits;
            }


            if (result.Contains("невозможен"))
            {
                return "false";
            }

            return result;
        }



        private static DateTime GetRegistrationDate(HtmlDocument doc)
        {
            DateTime result;
            HtmlNode node;
            string toBeSearched = "размещено";
            string value = String.Empty;
            string strDate = String.Empty;
            string format = "d MMMM";

            node = doc.DocumentNode.SelectSingleNode(DateXPath);
            value = node.InnerText;
            strDate = value.Substring(value.IndexOf(toBeSearched) + toBeSearched.Length);
            strDate = strDate.Substring(0, strDate.LastIndexOf("в"));
            strDate = strDate.Trim();

            if (!DateTime.TryParseExact(strDate, format, new CultureInfo("ru-ru", true), DateTimeStyles.AllowWhiteSpaces, out result))
            {
                switch (strDate)
                {
                    case "сегодня":
                        result = DateTime.Now.Date;
                        break;
                    case "вчера":
                        result = DateTime.Now.Date.AddDays(-1);
                        break;
                    case "позавчера":
                        result = DateTime.Now.Date.AddDays(-2);
                        break;
                }
            }

            return result;
        }




        private static HashSet<string> GetUrls(string url)
        {
            HashSet<string> result = new HashSet<string>();
            string href = string.Empty;
            string baseUrl = "https://www.avito.ru";
            HtmlWeb web = new HtmlWeb();

            try
            {
                HtmlDocument doc = web.Load(url);
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(UrlsXPath);

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        href = node.GetAttributeValue("href", string.Empty);

                        if (!string.IsNullOrWhiteSpace(href) && IsResumeUrl(href))
                        {
                            if (href.StartsWith("/"))
                            {
                                result.Add(baseUrl + href);
                            }
                            else
                                result.Add(href);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.For(ThisType).Error(ex);
            }

            return result;
        }



        private static bool IsResumeUrl(string url)
        {
            if (url.Contains("rezume") && !url.Contains("?") && !url.EndsWith("rezume"))
                return true;
            else
                return false;
        }


        public static bool IfUrlExists(string url)
        {
            bool result = false;
            var request = WebRequest.Create(url);
            WebResponse response = null;

            try
            {
                response = (WebResponse)request.GetResponse();
                result = true;
            }
            catch (WebException ex)
            {
                Log.For(ThisType).Error("ERROR with getting response time for: " + url, ex);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }

            return result;
        }




    }
}