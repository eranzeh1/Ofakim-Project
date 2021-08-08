using HtmlAgilityPack;
using Newtonsoft.Json;
using OfakimProject;
using OfakimProject.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace OfakimProject
{
    public class BL
    {
        Repository rep = new Repository();
        readonly static string yahooPath = @"https://finance.yahoo.com/quote/{0}=X?p={0}=X&.tsrc=fin-srch";
        readonly static string bloombergPath = @"https://www.bloomberg.com/quote/{0}:CUR";
        readonly static List<string> currenciesSymbols = ConfigurationManager.AppSettings["CurrenciesList"].Split(',').ToList<string>();
        static List<Currency> currencies;

        internal List<Currency> GetCurrencies(out bool changed)
        {
            currencies = new List<Currency>();
            changed = false;
            HtmlDocument htmlDoc = new HtmlDocument();

            foreach (var c in currenciesSymbols)
            {   
                WebClient client = new WebClient();
                MemoryStream ms = new MemoryStream(client.DownloadData(string.Format(yahooPath, c)));

                //Load html
                htmlDoc.Load(ms, Encoding.UTF8);
                string curXPath = "//*[@id='quote-header-info']/div[2]/div[1]/div[1]/h1";
                string curValueXPath = "//*[@id='quote-header-info']/div[3]/div[1]/div/span[1]";

                //ParseErrors is an ArrayList containing any errors from the Load statement
                if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Count() > 0)
                {
                    //Try to load backup html
                    ms = new MemoryStream(client.DownloadData(string.Format(bloombergPath, c)));
                    //Load html
                    htmlDoc.Load(ms, Encoding.UTF8);
                    curXPath = "*[@id='root']/div/div/section[2]/div[1]/div[2]/section[1]/section/section/section/div[1]/span[1]";
                    curValueXPath = "//*[@id='root']/div/div/section[2]/div[1]/div[2]/section[1]/section/h1/div/div";
                }
                else
                {
                    if (htmlDoc.DocumentNode != null)
                    {
                        //Get data by Xpath
                        string cur = htmlDoc.DocumentNode.SelectSingleNode(curXPath).InnerText;
                        cur = cur.Substring(0, cur.IndexOf('(') - 1);
                        double curValue = Convert.ToDouble(htmlDoc.DocumentNode.SelectSingleNode(curValueXPath).InnerText);

                        if (CurrencyChanged() || currencies.Count != currenciesSymbols.Count)
                        {
                            //Check if currency exist in list
                            if (currencies.Any(item => item.currency == cur))
                            {
                                int curIndex = currencies.FindIndex(item => item.currency == cur);
                                currencies[curIndex].value = curValue;
                            }
                            else
                            {
                                currencies.Add(new Currency
                                {
                                    currency = cur,
                                    value = curValue
                                });
                            }
                            //Save data in DB
                            rep.SaveData(cur, curValue);
                            changed = true;
                        }
                    }
                }
            }
            return currencies;
        }

        internal bool CurrencyChanged()
        {
            DataTable lastCurrencies = rep.GetLastData();
            foreach (var c in currencies)
            {
                DataRow[] filteredRows = lastCurrencies.Select($"cur like '{c.currency}'");
                if (Convert.ToDouble(filteredRows[0].ItemArray[1]) != c.value)
                    return true;
            }
            return false;
        }

        internal DataTable GetLastCurrencies()
        {
            return rep.GetLastData();
        }
    }
}