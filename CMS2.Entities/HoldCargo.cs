namespace CMS2.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("HoldCargo")]
    public partial class HoldCargo : BaseEntity
    {
        public Guid HoldCargoID { get; set; }

        public Guid UserID { get; set; }

        public DateTime HoldCargoDate { get; set; }

        [Required]
        [StringLength(20)]
        public string AirwayBillNo { get; set; }

        [Required]
        [StringLength(20)]
        public string Cargo { get; set; }

        public Guid ReasonID { get; set; }

        [Required]
        [StringLength(50)]
        public string Endorsedby { get; set; }

        public Guid StatusID { get; set; }

        [StringLength(100)]
        public string Notes { get; set; }

        public bool Uploaded { get; set; }
        
    }
}
