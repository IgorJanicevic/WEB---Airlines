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
    public class RezervacijeController : ApiController
    {
        private readonly string letoviFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/letovi.json");
        private readonly string rezervacijeFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/rezervacije.json");
        private readonly string korisniciFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/korisnici.json");

        // POST api/rezervacije/rezervisi
        [HttpPost]
        [Route("api/rezervacije/rezervisi")]
        public IHttpActionResult RezervisiLet([FromBody] Rezervacija novaRezervacija)
        {
            if (novaRezervacija == null)
            {
                return BadRequest("Podaci o rezervaciji nisu ispravni.");
            }

            // Učitavanje svih letova iz fajla
            List<Let> letovi = LoadLetoviFromFile();

            // Pronalaženje leta za rezervaciju // Hardkodovano
            Let letZaRezervaciju = letovi.FirstOrDefault(l => l.LetId == novaRezervacija.RezervacijaId);

            if (letZaRezervaciju == null)
            {
                return BadRequest("Let za rezervaciju nije pronađen.");
            }

            // Provera da li ima dovoljno slobodnih mesta
            if (novaRezervacija.BrojPutnika > letZaRezervaciju.BrojSlobodnihMesta)
            {
                return BadRequest("Nema dovoljno slobodnih mesta za rezervaciju.");
            }

            // Ažuriranje broja slobodnih i zauzetih mesta u letu
            letZaRezervaciju.BrojSlobodnihMesta -= novaRezervacija.BrojPutnika;
            letZaRezervaciju.BrojZauzetihMesta += novaRezervacija.BrojPutnika;

            // Dodavanje nove rezervacije
            novaRezervacija.RezervacijaId = GenerateRezervacijaId();
            novaRezervacija.UkupnaCena = novaRezervacija.BrojPutnika * letZaRezervaciju.Cena;
            novaRezervacija.Status = StatusRezervacije.Kreirana; // Pretpostavljeno stanje rezervacije

            List<Rezervacija> rezervacije = LoadRezervacijeFromFile();

            var korisnikKontoler = new KorisniciController();
            korisnikKontoler.DodajKorisnikuRezervaciju(novaRezervacija.Korisnik, novaRezervacija.RezervacijaId);


                        
            // Ako rezervacija tog leta postoji, potrebno je da je samo azuriramo
            foreach(Rezervacija rez in rezervacije)
            {
                if((rez.Korisnik.Equals(novaRezervacija.Korisnik)) && (novaRezervacija.Let.LetId==rez.Let.LetId) && (rez.Status==0)) {
                    rez.BrojPutnika += novaRezervacija.BrojPutnika;
                    SaveRezervacijeToFile(rezervacije);
                    SaveLetoviToFile(letovi);

                    return Ok("Uspešno ste rezervisali mesta.");
                }
            }
            rezervacije.Add(novaRezervacija);
            SaveRezervacijeToFile(rezervacije);

            // Ažuriranje letova u JSON fajlu
            SaveLetoviToFile(letovi);

            return Ok("Uspešno ste rezervisali mesta.");
        }

        [HttpGet]
        [Route("api/rezervacije")]
        public IHttpActionResult DohvatiRezervacije()
        {
            try
            {
                List<Rezervacija> rezervacije = LoadRezervacijeFromFile();
                foreach(Rezervacija r in rezervacije)
                {
                    //if ((r.Let.DatumVremeDolaska - DateTime.Now).TotalHours < 24)
                    //{
                    //    r.Let.Status = StatusLeta.Zavrsen;
                    //    r.Status = StatusRezervacije.Zavrsena;
                    //}

                    if(r.Let.DatumVremeDolaska < DateTime.Now)
                    {
                        r.Let.Status = StatusLeta.Zavrsen;
                        r.Status = StatusRezervacije.Zavrsena;
                    }

                    if (r.Let.Status == StatusLeta.Zavrsen) {
                        r.Status = StatusRezervacije.Zavrsena;
                    }
                    if(r.Status== StatusRezervacije.Zavrsena)
                        r.Let.Status= StatusLeta.Zavrsen;

                }
                return Ok(rezervacije.Where(rez=>rez.JeObrisana==false));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }



        [HttpPost]
        [Route("api/rezervacije/odobri/{rezervacijaId}")]
        public IHttpActionResult OdobriRezervaciju(int rezervacijaId)
        {
            try
            {
                var rezervacije = LoadRezervacijeFromFile();
                var rezervacija = rezervacije.FirstOrDefault(r => r.RezervacijaId == rezervacijaId);
                if (rezervacija == null)
                {
                    return NotFound();
                }

                rezervacija.Status = StatusRezervacije.Odobrena;
                SaveRezervacijeToFile(rezervacije);
                return Ok("Rezervacija uspešno odobrena!");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPut]
        [Route("api/rezervacije/otkazi/{rezervacijaId}")]
        public IHttpActionResult OtkaziRezervaciju(int rezervacijaId)
        {
            try
            {
                var rezervacije = LoadRezervacijeFromFile();
                var rezervacija = rezervacije.FirstOrDefault(r => r.RezervacijaId == rezervacijaId);

                if (rezervacija == null)
                {
                    return NotFound();
                }

                var datumIvremePolaska = rezervacija.Let.DatumVremePolaska;
                if ((datumIvremePolaska - DateTime.Now).TotalHours < 24)
                {
                    return BadRequest("Rezervaciju je moguće otkazati najkasnije do 24h pre vremena polaska leta.");
                }

                var letovi = LoadLetoviFromFile();
                foreach (Let l in letovi)
                {
                    if (l.LetId == rezervacija.Let.LetId)
                    {
                        l.BrojSlobodnihMesta += rezervacija.BrojPutnika;
                        l.BrojZauzetihMesta -= rezervacija.BrojPutnika;
                    }
                }
                SaveLetoviToFile(letovi);

                rezervacija.Status = StatusRezervacije.Otkazana;
                rezervacija.Let.Status = StatusLeta.Otkazan;
                SaveRezervacijeToFile(rezervacije);
                AzurirajRezervacijeTrenutniKorisnik(rezervacija.Korisnik, rezervacija.RezervacijaId);

                return Ok("Rezervacija je uspešno otkazana.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }





        private void AzurirajRezervacijeTrenutniKorisnik(string korIme,int rezId)
        {
            if (!File.Exists(korisniciFilePath))
            {
                return;
            }

            var json = File.ReadAllText(korisniciFilePath);
            var korisnici = JsonConvert.DeserializeObject<List<Korisnik>>(json);
            foreach(Korisnik kor in korisnici)
            {
                if (kor.KorisnickoIme.Equals(korIme))
                {
                        kor.ListaRezervacija.Add(rezId);
                }
            }
            string jsonData = JsonConvert.SerializeObject(korisnici, Formatting.Indented);
            File.WriteAllText(korisniciFilePath, jsonData);

        }

        private List<Let> LoadLetoviFromFile()
        {
            if (!File.Exists(letoviFilePath))
            {
                return new List<Let>();
            }

            var json = File.ReadAllText(letoviFilePath);
            return JsonConvert.DeserializeObject<List<Let>>(json);
        }

        private void SaveLetoviToFile(List<Let> letovi)
        {
            string jsonData = JsonConvert.SerializeObject(letovi, Formatting.Indented);
            File.WriteAllText(letoviFilePath, jsonData);
        }

        public List<Rezervacija> LoadRezervacijeFromFile()
        {
            if (!File.Exists(rezervacijeFilePath))
            {
                return new List<Rezervacija>();
            }
            string jsonData = File.ReadAllText(rezervacijeFilePath);
            return JsonConvert.DeserializeObject<List<Rezervacija>>(jsonData);
        }

        public void SaveRezervacijeToFile(List<Rezervacija> rezervacije)
        {
            string jsonData = JsonConvert.SerializeObject(rezervacije, Formatting.Indented);
            File.WriteAllText(rezervacijeFilePath, jsonData);
        }

        private int GenerateRezervacijaId()
        {
            Random random = new Random();
            return random.Next(1, 1200);
        }
    }
}
