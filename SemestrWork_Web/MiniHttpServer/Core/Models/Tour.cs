using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace MiniHttpServer.Core.Models
{
    public class Tour
    {       
        public int id { get; set; }
        public string image_url { get; set; }
        public string tour_name { get; set; }
        public string departure_city { get; set; }
        public string arrival_city { get; set; }
        public DateTime departure_date { get; set; }
        public int nights_count { get; set; }
        public int people_count { get; set; }
        public decimal tour_price { get; set; }
        public string hotel_name { get; set; }
        public string location_description { get; set; }
        public int rating { get; set; } = 5;
        public string meal_plan { get; set; } = "Все включено";
        
        public DateTime end_date { get; set; }                
        public string nearby_attractions { get; set; }       
        public string hotel_facilities { get; set; }           
        public int adult_pools_count { get; set; }            
        public int children_pools_count { get; set; }         
        public string beach_info { get; set; }                 
        public string contact_info { get; set; }               
        
        public string FormattedDate => departure_date.ToString("dd/MM/yyyy");
        public string FormattedEndDate => end_date.ToString("dd/MM/yyyy");
        public string PriceDisplay => $"{tour_price:N0} ₽";
        public string NightsDisplay => $"{nights_count} ночей";
        public string PeopleDisplay => $"{people_count} чел.";

        public string StarsDisplay
        {
            get
            {
                var stars = "";
                for (int i = 1; i <= 5; i++)
                    stars += i <= rating ? "★" : "☆";
                return stars;
            }
        }
    }
}