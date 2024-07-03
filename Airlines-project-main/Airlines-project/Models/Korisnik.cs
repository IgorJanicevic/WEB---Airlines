using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Airlines_project.Models
{
    public class Korisnik
    {
        public string KorisnickoIme { get; set; }
        public string Lozinka { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Email { get; set; }
        [JsonConverter(typeof(CustomDateFormatConverter))]
        public DateTime DatumRodjenja { get; set; }
        public Pol Pol { get; set; }
        public TipKorisnika TipKorisnika { get; set; }
        public List<int> ListaRezervacija { get; set; }
    }

    public enum Pol
    {
        Muski,
        Zenski
    }

    public enum TipKorisnika
    {
        Putnik,
        Administrator
    }
}

public class CustomDateFormatConverter : IsoDateTimeConverter
{
    public CustomDateFormatConverter()
    {
        DateTimeFormat = "yyyy-MM-dd";
    }
}
