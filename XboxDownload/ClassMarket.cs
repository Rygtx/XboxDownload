using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    class Market
    {
        public String name;
        public String code;
        public String language;

        public Market(String name, String code, String language)
        {
            this.name = name;
            this.code = code;
            this.language = language;
        }

        public override string ToString()
        {
            return this.name;
        }
    }

    class Product
    {
        public String title;
        public String id;

        public Product(String title, string id)
        {
            this.title = title;
            this.id = id;
        }

        public override string ToString()
        {
            return this.title;
        }
    }

    class ClassGame
    {
        public class Game
        {
            public List<Products> Products { get; set; }
        }

        public class Products
        {
            public DateTime LastModifiedDate { get; set; }
            public List<LocalizedProperties> LocalizedProperties { get; set; }
            public List<MarketProperties> MarketProperties { get; set; }
            public Properties Properties { get; set; }
            public List<DisplaySkuAvailabilities> DisplaySkuAvailabilities { get; set; }
            public string ProductId { get; set; }
        }

        public class LocalizedProperties
        {
            public string DeveloperName { get; set; }
            public string PublisherName { get; set; }
            public EligibilityProperties EligibilityProperties { get; set; }
            public List<Images> Images { get; set; }
            public string ProductDescription { get; set; }
            public string ProductTitle { get; set; }
            public string[] Markets { get; set; }
        }

        public class MarketProperties
        {
            public DateTime OriginalReleaseDate { get; set; }
        }

        public class EligibilityProperties
        {
            public Affirmations[] Affirmations { get; set; }
        }

        public class Affirmations
        {
            public string Description { get; set; }
        }

        public class Images
        {
            public string ImagePurpose { get; set; }
            public string Uri { get; set; }
            public int Height { get; set; }
            public int Width { get; set; }
        }

        public class DisplaySkuAvailabilities
        {
            public Sku Sku { get; set; }
            public List<Availabilities> Availabilities { get; set; }
        }

        public class Sku
        {
            public Properties Properties { get; set; }
            public string SkuType { get; set; }
        }

        public class Properties
        {
            public string Category { get; set; }
            public List<Packages> Packages { get; set; }
            public List<BundledSkus> BundledSkus { get; set; }

            //EA Play
            public string[] MerchandisingTags { get; set; }
        }

        public class Packages
        {
            public ulong MaxDownloadSizeInBytes { get; set; }
            public string[] Languages { get; set; }          
            public string PackageFormat { get; set; }
            public string PackageFullName { get; set; }
            public string ContentId { get; set; }
            public int PackageRank { get; set; }
            public List<PlatformDependencies> PlatformDependencies { get; set; }
            public List<PackageDownloadUris> PackageDownloadUris { get; set; }
        }

        public class BundledSkus
        {
            public string BigId { get; set; }
        }

        public class PlatformDependencies
        {
            public string PlatformName { get; set; }
        }

        public class PackageDownloadUris
        {
            public string Uri { get; set; }
        }


        public class Availabilities
        {
            public Conditions Conditions { get; set; }
            public OrderManagementData OrderManagementData { get; set; }
            public Properties Properties { get; set; }
        }

        public class Conditions
        {
            public DateTime EndDate { get; set; }
            public DateTime StartDate { get; set; }
        }
        public class OrderManagementData
        {
            public Price Price { get; set; }
        }

        public class Price
        {
            public string CurrencyCode { get; set; }
            public double MSRP { get; set; }
            public double ListPrice { get; set; }
            public double WholesalePrice { get; set; }
        }

        //Search
        public class Search
        {
            public string Query { get; set; }
            public List<ResultSets> ResultSets { get; set; }
        }

        public class ResultSets
        {
            public List<Suggests> Suggests { get; set; }
        }

        public class Suggests
        {
            public string Source { get; set; }
            public string Title { get; set; }
            public List<Metas> Metas { get; set; }
        }
        public class Metas
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }


        public static void ExchangeRate(string CurrencyCode)
        {
            if (CurrencyCode == "CNY") return;
            SocketPackage socketPackage;
            Match result;

            switch (CurrencyCode)
            {
                case "BDT":
                case "CAD":
                case "GTQ":
                case "KZT":
                case "MRO":
                case "NGN":
                case "PEN":
                case "PHP":
                case "PKR":
                case "TND":
                case "TTD":
                case "UAH":
                    break;
                default:
                    socketPackage = ClassWeb.HttpRequest("https://www.baidu.com/s?wd=" + CurrencyCode + "%20CNY", "GET", null, null, true, false, true, "utf-8", null, null, ClassWeb.useragent, null, null, null, null, 0, null);
                    result = Regex.Match(socketPackage.Html, @"<div>1[^=]+=(?<ExchangeRate>.+)人民币</div>");
                    if (result.Success)
                    {
                        if (double.TryParse(result.Groups["ExchangeRate"].Value, out double ExchangeRate))
                        {
                            Form1.dicExchangeRate.AddOrUpdate(CurrencyCode, ExchangeRate, (oldkey, oldvalue) => ExchangeRate);
                            return;
                        }
                    }
                    break;
            }

            socketPackage = ClassWeb.HttpRequest("https://www.majorexchangerates.com/" + CurrencyCode.ToLowerInvariant() + "/cny.html", "GET", null, null, true, false, true, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
            result = Regex.Match(socketPackage.Html, @"<a [^>]+>1 " + CurrencyCode + " = (?<ExchangeRate>.+) CNY</a>");
            if (result.Success)
            {
                if (double.TryParse(result.Groups["ExchangeRate"].Value, out double ExchangeRate))
                {
                    Form1.dicExchangeRate.AddOrUpdate(CurrencyCode, ExchangeRate, (oldkey, oldvalue) => ExchangeRate);
                    return;
                }
            }

            socketPackage = ClassWeb.HttpRequest("https://www.convertworld.com/zh-hans/currency/mauritania/" + CurrencyCode.ToLowerInvariant() + "-cny.html", "GET", null, null, true, false, true, null, null, null, ClassWeb.useragent, null, null, null, null, 0, null);
            result = Regex.Match(socketPackage.Html, @"一个" + CurrencyCode + "是(?<ExchangeRate>.+) CNY");
            if (result.Success)
            {
                if (double.TryParse(result.Groups["ExchangeRate"].Value, out double ExchangeRate))
                {
                    Form1.dicExchangeRate.AddOrUpdate(CurrencyCode, ExchangeRate, (oldkey, oldvalue) => ExchangeRate);
                    return;
                }
            }
        }
    }

    class XboxGameDownload
    {
        public static ConcurrentDictionary<String, Products> dicXboxGame = new ConcurrentDictionary<String, Products>();

        [Serializable]
        public class Products
        {
            public Version Version { get; set; }
            public ulong FileSize { get; set; }
            public string Url { get; set; }
        }

        public class Game
        {
            public bool PackageFound { get; set; }
            public string ContentId { get; set; }
            public List<PackageFiles> PackageFiles { get; set; }
        }

        public class PackageFiles
        {
            public ulong FileSize { get; set; }
            public string[] CdnRootPaths { get; set; }
            public string RelativeUrl { get; set; }
            public DateTime ModifiedDate { get; set; }
        }
    }

    class MsAppDownload
    {
        public static ConcurrentDictionary<String, Products> dicMsApp = new ConcurrentDictionary<String, Products>();
        public class Products
        {
            public string Url { get; set; }
            public string Filename { get; set; }
            public DateTime Expire { get; set; }
        }
    }

    class PsGame
    {
        public class Game
        {
            public long OriginalFileSize { get; set; }
            public int NumberOfSplitFiles { get; set; }
            public List<Pieces> Pieces { get; set; }
        }

        public class Pieces
        {
            public string Url { get; set; }
        }
    }
}
