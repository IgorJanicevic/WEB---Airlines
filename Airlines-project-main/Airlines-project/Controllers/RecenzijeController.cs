using Airlines_project.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Airlines_project.Controllers
{
    public class RecenzijeController : ApiController
    {
        private string recenzijeFilePath = HttpContext.Current.Server.MapPath("~/App_Data/recenzije.json");
        private readonly string kompanijeFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/aviokompanije.json");


        private List<Recenzija> UcitajRecenzijeIzFajla()
        {
            if (!File.Exists(recenzijeFilePath))
            {
                return new List<Recenzija>();
            }

            var json = File.ReadAllText(recenzijeFilePath);
            return JsonConvert.DeserializeObject<List<Recenzija>>(json);
        }


        private void SaveRecenzijeToFile(List<Recenzija> recenzije)
        {
            string json = JsonConvert.SerializeObject(recenzije, Formatting.Indented);
            File.WriteAllText(recenzijeFilePath, json);
        }

        // GET: api/recenzije
        [HttpGet]
        [Route("api/recenzije")]
        public IHttpActionResult DohvatiRecenzije()
        {
            try
            {
                var recenzije = UcitajRecenzijeIzFajla();
                recenzije.Where(rec => rec.JeObrisana == false);


                return Ok(recenzije.Where(rec=>rec.JeObrisana==false));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPost]
        [Route("api/recenzije")]
        public IHttpActionResult CreateRecenzija(Recenzija recenzija)
        {
            try
            {
                List<Recenzija> recenzije = UcitajRecenzijeIzFajla();

                // Generisanje jedinstvenog ID-ja za novu recenziju
                recenzija.RecenzijaId = recenzije.Count > 0 ? recenzije.Max(r => r.RecenzijaId) + 1 : 1;
                recenzija.JeObrisana = false;
                recenzije.Add(recenzija);

                SaveRecenzijeToFile(recenzije);
                DodajRecenzijuAvioKompaniji(recenzija.AviokompanijaId, recenzija.RecenzijaId);



                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }



        // POST: api/recenzije/odobri/{recenzijaId}
        [HttpPost]
        [Route("api/recenzije/odobri/{recenzijaId}")]
        public IHttpActionResult OdobriRecenziju(int recenzijaId)
        {
            try
            {
                var recenzije = UcitajRecenzijeIzFajla();
                var recenzija = recenzije.FirstOrDefault(r => r.RecenzijaId == recenzijaId);

                if (recenzija == null)
                {
                    return NotFound();
                }

                recenzija.Status = StatusRecenzije.Odobrena;
                SaveRecenzijeToFile(recenzije);

                return Ok("Recenzija je uspešno odobrena.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST: api/recenzije/odbij/{recenzijaId}
        [HttpPost]
        [Route("api/recenzije/odbij/{recenzijaId}")]
        public IHttpActionResult OdbijRecenziju(int recenzijaId)
        {
            try
            {
                var recenzije = UcitajRecenzijeIzFajla();
                var recenzija = recenzije.FirstOrDefault(r => r.RecenzijaId == recenzijaId);

                if (recenzija == null)
                {
                    return NotFound();
                }

                recenzija.Status = StatusRecenzije.Odbijena;
                SaveRecenzijeToFile(recenzije);

                return Ok("Recenzija je uspešno odbijena.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        public void DodajRecenzijuAvioKompaniji(int aviokompanijaId,int recenzijaId)
        {
            List<Aviokompanija> aviokompanije = LoadAviokompanijeFromFile();

            Aviokompanija aviokompanija = aviokompanije.FirstOrDefault(av=>av.AviokompanijaId==aviokompanijaId);


            if (aviokompanija != null)
            {
                aviokompanija.Recenzije.Add(recenzijaId);

                SaveAviokompanijeToFile(aviokompanije);
            }
            else
            {
                throw new Exception("Aviokompanija sa zadatim ID nije pronađena.");
            }
        }

        private List<Aviokompanija> LoadAviokompanijeFromFile()
        {
            if (!File.Exists(kompanijeFilePath))
            {
                return new List<Aviokompanija>();
            }

            string json = File.ReadAllText(kompanijeFilePath);
            return JsonConvert.DeserializeObject<List<Aviokompanija>>(json);
        }

        private void SaveAviokompanijeToFile(List<Aviokompanija> aviokompanije)
        {
            string json = JsonConvert.SerializeObject(aviokompanije, Formatting.Indented);
            File.WriteAllText(kompanijeFilePath, json);
        }

    }
}
