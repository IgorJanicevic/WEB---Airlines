using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airlines_project.Models
{
    public class Recenzija
    {
        public int RecenzijaId { get; set; }
        public string Recezent { get; set; } 
        public string Naslov { get; set; }
        public string Sadrzaj { get; set; }
        public string Slika { get; set; }
        public int AviokompanijaId { get; set; }
        public StatusRecenzije Status { get; set; }
        public bool JeObrisana { get; set; }
    }

    public enum StatusRecenzije
    {
        Kreirana,
        Odobrena,
        Odbijena
    }
}