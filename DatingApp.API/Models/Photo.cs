using System;

namespace DatingApp.API.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsMain { get; set; }
        public string PublicId { get; set; }

        // By Convention EF will recognise this line and the one below to assign a many to many relationship. Meaning both User and Photo data will be deleted together.
        public virtual User User { get; set; } 
        public int UserId { get; set; }
    }
}