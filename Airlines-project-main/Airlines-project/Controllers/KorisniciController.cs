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
    public class KorisniciController : ApiController
    {
        private readonly string korisniciFilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/korisnici.json");

        // POST api/korisnici/registracija
        [HttpPost]
        [Route("api/korisnici/registracija")]
        public IHttpActionResult Registracija([FromBody] Korisnik noviKorisnik)
        {
            if (noviKorisnik == null)
            {
                return BadRequest("Podaci o korisniku nisu ispravni.");
            }

            if(noviKorisnik.DatumRodjenja > DateTime.Now)
            {
                return BadRequest("Datum rodjenja je nevalidan!");
            }

            List<Korisnik> korisnici = LoadKorisniciFromFile();

            // Provera da li korisnicko ime vec postoji
            if (korisnici.Any(k => k.KorisnickoIme.Equals(noviKorisnik.KorisnickoIme)))
            {
                return BadRequest("Korisničko ime već postoji. Molimo izaberite drugo korisničko ime.");
            }
            noviKorisnik.ListaRezervacija = new List<int>();
            korisnici.Add(noviKorisnik);
            SaveKorisniciToFile(korisnici);

            return Ok(noviKorisnik);
        }

        [HttpPost]
        [Route("api/korisnici/prijava")]
        public IHttpActionResult Prijava([FromBody] Korisnik loginData)
        {
            if (loginData == null || string.IsNullOrWhiteSpace(loginData.KorisnickoIme) || string.IsNullOrWhiteSpace(loginData.Lozinka))
            {
                return BadRequest("Podaci o korisniku nisu ispravni.");
            }

            List<Korisnik> korisnici = LoadKorisniciFromFile();

            Korisnik korisnik = korisnici.FirstOrDefault(kor => kor.KorisnickoIme.Equals(loginData.KorisnickoIme) && kor.Lozinka.Equals(loginData.Lozinka));

            if (korisnik == null)
            {
                return BadRequest("Pogrešno korisničko ime ili lozinka. Molimo pokušajte ponovo.");
            }

            return Ok(korisnik); // Vracam JSON objekat korisnika
        }




        // GET api/korisnici/{korisnickoIme}
        [HttpGet]
        [Route("api/korisnici/{korisnickoIme}")]
        public IHttpActionResult GetKorisnikByKorisnickoIme(string korisnickoIme)
        {
            
            List<Korisnik> korisnici = LoadKorisniciFromFile();
            Korisnik korisnik = korisnici.FirstOrDefault(k => k.KorisnickoIme.Equals(korisnickoIme.ToString()));

            if (korisnik == null)
            {
                return BadRequest("Greska prilikom preuzimanja korisnika iz baze");
                //return Ok(korisnici[0]);
            }

            return Ok(korisnik);
        }
        [HttpGet]
        [Route("api/korisnici")]
        public IHttpActionResult PreuzmiKorisnike()
        {
            List<Korisnik> korisnici = LoadKorisniciFromFile();
            return Ok(korisnici);
        }

        [HttpPut]
        [Route("api/korisnici/azuriraj")]
        public IHttpActionResult AzurirajKorisnika([FromBody] Korisnik azuriraniKorisnik)
        {
            if (azuriraniKorisnik == null || string.IsNullOrWhiteSpace(azuriraniKorisnik.KorisnickoIme))
            {
                return BadRequest("Podaci o korisniku nisu ispravni.");
            }

            List<Korisnik> korisnici = LoadKorisniciFromFile();

            //if (korisnici.Where(kor => kor.KorisnickoIme.Equals(azuriraniKorisnik.KorisnickoIme)) != null)
            //{
            //    return BadRequest("Korisnicko ime je vec zauzeto");
            //}

            if(azuriraniKorisnik.DatumRodjenja > DateTime.Now)
            {
                return BadRequest("Datum rodjenja je nevalidan!");
            }

            Korisnik korisnik = korisnici.FirstOrDefault(k => k.KorisnickoIme.Equals(azuriraniKorisnik.KorisnickoIme));
            if (korisnik == null)
            {
                return BadRequest("Korisnik nije pronađen.");
            }

            korisnik.Ime = azuriraniKorisnik.Ime;
            korisnik.Prezime = azuriraniKorisnik.Prezime;
            korisnik.Email = azuriraniKorisnik.Email;
            korisnik.DatumRodjenja = azuriraniKorisnik.DatumRodjenja.Date; // Postavljanje samo datuma
            korisnik.Pol = azuriraniKorisnik.Pol;

            SaveKorisniciToFile(korisnici);

            return Ok("Profil uspešno ažuriran.");
        }

        //  Pomocne funkcije za rad sa fajlom 



        public List<Korisnik> LoadKorisniciFromFile()
        {
            string jsonData = File.ReadAllText(korisniciFilePath);
            return JsonConvert.DeserializeObject<List<Korisnik>>(jsonData);
        }

        public void SaveKorisniciToFile(List<Korisnik> korisnici)
        {
            string jsonData = JsonConvert.SerializeObject(korisnici, Formatting.Indented);
            File.WriteAllText(korisniciFilePath, jsonData);
        }

        public void DodajKorisnikuRezervaciju(string korIme, int rezervacijaId)
        {
            List<Korisnik> korisnici = LoadKorisniciFromFile();

            Korisnik korisnik = korisnici.FirstOrDefault(k => k.KorisnickoIme.Equals(korIme, StringComparison.OrdinalIgnoreCase));

            if (korisnik != null)
            {
                if (korisnik.ListaRezervacija == null)
                {
                    korisnik.ListaRezervacija = new List<int>();
                }

                korisnik.ListaRezervacija.Add(rezervacijaId);

                SaveKorisniciToFile(korisnici);
            }
            else
            {
                throw new Exception("Korisnik nije pronađen.");
            }
        }
    }
}
