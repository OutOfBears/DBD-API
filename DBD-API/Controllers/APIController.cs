using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBD_API.Services;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DBD_API.Controllers
{
    public class APIController : Controller
    {
        private DBDService dbdService;

        public APIController(
            DBDService _dbdService
        )
        {
            this.dbdService = _dbdService;
        }

        
        public async Task<ActionResult> ShrineOfSecrets()
        {
            var shrine = await dbdService.GetShrine();

            if (!string.IsNullOrEmpty(Request.Query["pretty"]))
            {
                if(shrine != null)
                {
                    try
                    {
                        var output = new StringBuilder();

                        var items = shrine["items"].ToArray();
                        var start = (DateTime)shrine["startDate"];
                        var end = (DateTime)shrine["endDate"];

                        foreach(var item in items)
                        {
                            var name = (string)item["id"];
                            var cost = item["cost"].ToArray();
                            if (cost.Count() <= 0)
                                continue;

                            var price = (int)cost[0]["price"];

                            output.Append(string.Format("{0} : {1}", name, price));

                            if (item != items.Last())
                                output.Append(", ");
                        }

                        output.Append(" | ");

                        var changesIn = end - DateTime.Now;
                        output.Append(string.Format("Shrine changes in {0} days, {1} hours, and {2} mins",
                            changesIn.Days,
                            changesIn.Hours,
                            changesIn.Minutes
                        ));

                        return Content(output.ToString());
                    }
                    catch
                    {
                        return Content("Uhhhh, we failed to parse the data retrieved, contact me to fix");
                    }
                }
                else
                {
                    return Content("Uh oh, we failed to retrieve the shrine from dbd servers :/");
                }
            }
            else
            {
                return Json(shrine);
            }
        }

        public async Task<ActionResult> StoreOutfits()
        {
            return Json(await dbdService.GetStoreOutfits());
        }
    }
}
