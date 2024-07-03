using Airlines_project.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Airlines_project.Controllers
{
    public class AviokompanijeController : ApiController
    {
        private readonly string kompanijeFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/aviokompanije.json");

        public AviokompanijeController()
        {
            if (!File.Exists(kompanijeFilePath))
            {
                File.Create(kompanijeFilePath);
            }
        }

        [HttpGet]
        [Route("api/aviokompanije")]
        public IHttpActionResult DohvatiKompanie()
        {
            try
            {
                var kompanije = UcitajKompanijeIzFajla();
                var postojeKompanije = kompanije.Where(kp => kp.JeObrisana == false);
                return Ok(postojeKompanije);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public List<Aviokompanija> UcitajKompanijeIzFajla()
        {       

            var json = File.ReadAllText(kompanijeFilePath);
            return JsonConvert.DeserializeObject<List<Aviokompanija>>(json);
        }

        public void SacuvajKompanijeUFajl(List<Aviokompanija> kompanije)
        {
            var json = JsonConvert.SerializeObject(kompanije, Formatting.Indented);
            File.WriteAllText(kompanijeFilePath, json);
        }


        [HttpGet]
        [Route("api/aviokompanije/{id}")]
        public IHttpActionResult DohvatiKompaniju(int id)
        {
                var kompanije = UcitajKompanijeIzFajla();
                var kompTemp = kompanije[1];
                var kompanija = kompanije.FirstOrDefault(k => k.AviokompanijaId == id);
                if (kompanija == null || kompanija.JeObrisana==true)
                {
                    return NotFound();
                }
            return Ok(kompanija);
           
            
        }


        [HttpGet]
        [Route("api/aviokompanije/let/{letId}")]
        public IHttpActionResult GetAviokompanijaIdByLetId(int letId)
        {
            List<Aviokompanija> aviokompanije = UcitajKompanijeIzFajla();
            Aviokompanija aviokompanija = null;
            foreach(Aviokompanija ak in aviokompanije)
            {
                if (ak.Letovi.Contains(letId))
                {
                    aviokompanija = ak;break;
                }
            }

            if (aviokompanija == null || aviokompanija.JeObrisana==true)
            {
                return NotFound();
            }

            return Ok(aviokompanija);
        }

        [HttpPost]
        [Route("api/aviokompanije/update")]
        public IHttpActionResult UpdateAviokompanija(Aviokompanija updatedAviokompanija)
        {
            try
            {
                List<Aviokompanija> aviokompanije = UcitajKompanijeIzFajla();
                Aviokompanija existingAviokompanija = aviokompanije.FirstOrDefault(a => a.AviokompanijaId == updatedAviokompanija.AviokompanijaId);

                if (existingAviokompanija == null)
                {
                    return NotFound();
                }

                // Ažuriranje aviokompanije
                existingAviokompanija.Recenzije = updatedAviokompanija.Recenzije;
                existingAviokompanija.Naziv = updatedAviokompanija.Naziv;
                existingAviokompanija.KontaktInformacije = updatedAviokompanija.KontaktInformacije;
                existingAviokompanija.Adresa = updatedAviokompanija.Adresa;

                SacuvajKompanijeUFajl(aviokompanije);

                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("api/aviokompanije/add")]
        public IHttpActionResult DodajAviokompaniju([FromBody] Aviokompanija novaAviokompanija)
        {
            try
            {
                List<Aviokompanija> aviokompanije = UcitajKompanijeIzFajla();

                if (novaAviokompanija.Naziv == "" || novaAviokompanija.Adresa == "" || novaAviokompanija.KontaktInformacije == "")
                    return BadRequest("Sva polja moraju biti popunjena");

                // Generisanje novog AviokompanijaId-a
                int noviId = aviokompanije.Count > 0 ? aviokompanije.Max(a => a.AviokompanijaId) + 1 : 1;
                novaAviokompanija.AviokompanijaId = noviId;
                novaAviokompanija.Letovi= new List<int>();
                novaAviokompanija.Recenzije= new List<int>() { };
                novaAviokompanija.JeObrisana = false;

                // Dodavanje nove aviokompanije u listu
                aviokompanije.Add(novaAviokompanija);

                // Čuvanje promena u fajl
                SacuvajKompanijeUFajl(aviokompanije);

                return Ok("Aviokompanija uspešno dodata!");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("api/aviokompanije/{aviokompanijaId}")]
        public IHttpActionResult ObrisiAviokompaniju(int aviokompanijaId)
        {
            try
            {
                List<Aviokompanija> aviokompanije = UcitajKompanijeIzFajla();

                // Pronaci aviokompaniju koju treba obrisati
                Aviokompanija aviokompanijaZaBrisanje = aviokompanije.FirstOrDefault(a => a.AviokompanijaId == aviokompanijaId);

                if (aviokompanijaZaBrisanje == null)
                {
                    return NotFound();
                }

                // Obrisi id iz letovi.json
                List<Let> letovi = UcitajLetoveIzFajla();
                List<Let> letoviZaBrisanje= new List<Let>();
                foreach (var let in letovi.ToList())
                {
                    if (aviokompanijaZaBrisanje.Letovi.Contains(let.LetId))
                    {
                        if(let.Status== StatusLeta.Aktivan && let.JeObrisana==false)
                        {
                            return BadRequest("Ne mozete obrisati aviokompaniju sa aktivnim letovima!");
                        }
                        letoviZaBrisanje.Add(let);
                    }
                }
                foreach (var let in letoviZaBrisanje) //letovi.Remove(let);
                    let.JeObrisana = true;

                SacuvajLetoveUFajl(letovi);

                // Obrisi id iz recenzije.json
                List<Recenzija> recenzije = UcitajRecenzijeIzFajla();
                foreach (var recenzija in recenzije.ToList())
                {
                    if (aviokompanijaZaBrisanje.Recenzije != null)
                    {
                        if (aviokompanijaZaBrisanje.Recenzije.Contains(recenzija.RecenzijaId))
                        {
                            recenzija.JeObrisana = true;
                            
                        }
                    }
                }
                SacuvajRecenzijeUFajl(recenzije);

                var rezervacijeKontroler = new RezervacijeController();
                List<Rezervacija> rezervacije= rezervacijeKontroler.LoadRezervacijeFromFile();
                foreach(Rezervacija rez in rezervacije)
                {
                    if (aviokompanijaZaBrisanje.Letovi.Contains(rez.Let.LetId))
                    {
                        rez.Let.JeObrisana = true;
                        rez.JeObrisana = true;
                    }
                }
                rezervacijeKontroler.SaveRezervacijeToFile(rezervacije);


                aviokompanijaZaBrisanje.JeObrisana = true;

                // Čuvanje promena u fajl
                SacuvajKompanijeUFajl(aviokompanije);

                return Ok("Aviokompanija uspešno obrisana!");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        

        private List<Let> UcitajLetoveIzFajla()
        {
            string letoviFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/letovi.json");
            var json = File.ReadAllText(letoviFilePath);
            return JsonConvert.DeserializeObject<List<Let>>(json);
        }

        private void SacuvajLetoveUFajl(List<Let> letovi)
        {
            string letoviFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/letovi.json");
            var json = JsonConvert.SerializeObject(letovi, Formatting.Indented);
            File.WriteAllText(letoviFilePath, json);
        }

        private List<Recenzija> UcitajRecenzijeIzFajla()
        {
            string recenzijeFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/recenzije.json");
            var json = File.ReadAllText(recenzijeFilePath);
            return JsonConvert.DeserializeObject<List<Recenzija>>(json);
        }

        private void SacuvajRecenzijeUFajl(List<Recenzija> recenzije)
        {
            string recenzijeFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/recenzije.json");
            var json = JsonConvert.SerializeObject(recenzije, Formatting.Indented);
            File.WriteAllText(recenzijeFilePath, json);
        }


    }
}
