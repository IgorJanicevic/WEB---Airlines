using Airlines_project.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Hosting;
using System.Web.Http;

namespace Airlines_project.Controllers
{
    [Route("api/letovi")]
    public class LetoviController : ApiController
    {
        private readonly string letoviFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/letovi.json");

        public LetoviController()
        {
            if (!File.Exists(letoviFilePath))
            {
                File.Create(letoviFilePath);
            }
        }

        [HttpGet]
        [Route("api/letovi")]
        public IHttpActionResult DohvatiLetove()
        {
            try
            {
                var letovi = UcitajLetoveIzFajla();
                foreach(Let l in letovi)
                {
                    if(l.DatumVremeDolaska< DateTime.Now)
                    {
                        l.Status = StatusLeta.Zavrsen;
                    }
                }
                return Ok(letovi.Where(let=>let.JeObrisana==false));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpGet]
        [Route("api/letovi/{letId}")]
        public IHttpActionResult DohvatiLet(int letId)
        {
            try
            {
                var letovi = UcitajLetoveIzFajla();
                foreach(Let l in letovi)
                {
                    if (l.LetId == letId && l.JeObrisana==false)
                        return Ok(l);
                }
                return NotFound();

            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("api/letovi/azuriraj")]
        public IHttpActionResult AzurirajLet([FromBody] Let azuriraniLet)
        {
            try
            {
                List<Let> letovi = UcitajLetoveIzFajla();
                var stariLet = letovi.FirstOrDefault(l => l.LetId == azuriraniLet.LetId);

                if (stariLet == null)
                {
                    return NotFound();
                }

                var rezervacijaKontoler = new RezervacijeController();

                var rezervacije = rezervacijaKontoler.LoadRezervacijeFromFile();

                if (stariLet.Cena != azuriraniLet.Cena)
                {
                    foreach (Rezervacija r in rezervacije)
                    {
                        if (r.Let.LetId == azuriraniLet.LetId && (r.Status == StatusRezervacije.Kreirana || r.Status == StatusRezervacije.Odobrena))
                        {
                            return BadRequest("Ne mozete izmeniti cenu leta koja vec poseduje aktivne rezervacije!");
                        }
                    }
                }

                

                // Ažuriranje informacija o letu
                stariLet.DatumVremePolaska = azuriraniLet.DatumVremePolaska;
                stariLet.DatumVremeDolaska = azuriraniLet.DatumVremeDolaska;
                stariLet.Cena = azuriraniLet.Cena;
                stariLet.BrojSlobodnihMesta = azuriraniLet.BrojSlobodnihMesta;
                stariLet.BrojZauzetihMesta = azuriraniLet.BrojZauzetihMesta;
                bool menjaj=false;

                if (stariLet.DatumVremeDolaska < DateTime.Now && stariLet.Status== StatusLeta.Aktivan)
                {
                    stariLet.Status = StatusLeta.Zavrsen;
                    menjaj=true;
                }
                

                



                foreach (Rezervacija r in rezervacije)
                {
                    if (r.Let.LetId == stariLet.LetId)
                    {
                        var temp = r.Let.LetId;
                        
                        r.Let = stariLet;
                        r.Let.LetId= temp;
                        if(menjaj)
                        {
                            r.Status = StatusRezervacije.Zavrsena;
                        }
                    }
                }

                rezervacijaKontoler.SaveRezervacijeToFile(rezervacije);

                SacuvajLetoveUFajl(letovi);

                return Ok("Let uspešno ažuriran!");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("api/letovi/dodaj")]
        public IHttpActionResult DodajLet([FromBody] Let noviLet)
        {
            try
            {
                List<Let> letovi = UcitajLetoveIzFajla();
                var aviokompanijaId = noviLet.LetId;

                noviLet.LetId = letovi.Count > 0 ? letovi.Max(l => l.LetId) + 1 : 1;
                noviLet.JeObrisana = false;

                noviLet.Status = StatusLeta.Aktivan;
                noviLet.BrojZauzetihMesta = 0;

                letovi.Add(noviLet);
                var avioKontoler = new AviokompanijeController();
                var aviokompanije = avioKontoler.UcitajKompanijeIzFajla();
                var pronadjena = aviokompanije.FirstOrDefault(av => av.AviokompanijaId == aviokompanijaId);
                pronadjena.Letovi.Add(noviLet.LetId);

                noviLet.Aviokompanija = pronadjena.Naziv;
                avioKontoler.SacuvajKompanijeUFajl(aviokompanije);

                SacuvajLetoveUFajl(letovi);

                return Ok("Let uspešno dodat!");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("api/letovi/obrisi/{letId}")]
        public IHttpActionResult ObrisiLet(int letId)
        {
            try
            {
                List<Let> letovi = UcitajLetoveIzFajla();
                Let letZaBrisanje = letovi.FirstOrDefault(l => l.LetId == letId);

                if (letZaBrisanje == null)
                {
                    return NotFound();
                }

                var rezervacijeKontoler = new RezervacijeController();
                var rezervacije = rezervacijeKontoler.LoadRezervacijeFromFile();
                foreach(Rezervacija r in rezervacije)
                {
                    if(r.Let.LetId== letId && (r.Status== StatusRezervacije.Odobrena || r.Status== StatusRezervacije.Kreirana)) {
                        return BadRequest("Ne mozete obrisati let koji ima aktivne rezervacije!");
                    }
                }

                Rezervacija rez= rezervacije.FirstOrDefault(r=>r.Let.LetId== letId);
                if (rez !=null)
                {
                    rez.Let.JeObrisana = true;
                    rez.JeObrisana = true;
                }
                letZaBrisanje.JeObrisana = true;
                //letovi.Remove(letZaBrisanje);
                SacuvajLetoveUFajl(letovi);

                // uklonim let iz liste letova u pripadajućoj aviokompaniji
                var avioKontroler = new AviokompanijeController();
                var aviokompanije = avioKontroler.UcitajKompanijeIzFajla();
                foreach (var aviokompanija in aviokompanije)
                {
                    if (aviokompanija.Letovi.Contains(letId))
                    {
                        //aviokompanija.Letovi.Remove(letId);                       
                        avioKontroler.SacuvajKompanijeUFajl(aviokompanije);
                        break; 
                    }
                }

                return Ok("Let uspešno obrisan!");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

    public List<Let> UcitajLetoveIzFajla()
        {
            if (!File.Exists(letoviFilePath))
            {
                return new List<Let>();
            }

            var json = File.ReadAllText(letoviFilePath);
            return JsonConvert.DeserializeObject<List<Let>>(json);
        }
    private void SacuvajLetoveUFajl(List<Let> letovi)
        {
            var json = JsonConvert.SerializeObject(letovi, Formatting.Indented);
            File.WriteAllText(letoviFilePath, json);
        }

      
    }
}
