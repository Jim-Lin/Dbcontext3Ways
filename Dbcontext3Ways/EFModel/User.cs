namespace Dbcontext3Ways.EFModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Table("acct_user")]
    class User
    {
        [Column("acct_user_id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Column("acct_company_id", Order = 10)]
        [Index("ux_acct_user_username_companyid", 1, IsUnique = true)]
        public int CompanyID { get; set; }

        [Column("username", Order = 110)]
        [Index("ux_acct_user_username_companyid", 2, IsUnique = true)]
        [MaxLength(100)]
        [MinLength(3)]
        [Required]
        public string Username { get; set; }

        [Column("password", Order = 120)]
        [MaxLength(300)]
        [MinLength(8)]
        [Required]
        public string Password { get; set; }

        [Column("email", Order = 130)]
        [MaxLength(1000)]
        [MinLength(5)]
        [Required]
        public string Email { get; set; }

        [Column("name", Order = 140)]
        [MaxLength(300)]
        [Required]
        public string Name { get; set; }

        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; }
    }
}
