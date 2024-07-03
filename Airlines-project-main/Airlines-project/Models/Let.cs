using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airlines_project.Models
{
    public class Let
    {
        public int LetId { get; set; }
        public string Aviokompanija { get; set; }
        public string PolaznaDestinacija { get; set; }
        public string OdredisnaDestinacija { get; set; }
        public DateTime DatumVremePolaska { get; set; }
        public DateTime DatumVremeDolaska { get; set; }
        public int BrojSlobodnihMesta { get; set; }
        public int BrojZauzetihMesta { get; set; }
        public decimal Cena { get; set; }
        public StatusLeta Status { get; set; }
        public bool JeObrisana { get; set; }

    }

    public enum StatusLeta
    {
        Aktivan,
        Otkazan,
        Zavrsen
    }

}