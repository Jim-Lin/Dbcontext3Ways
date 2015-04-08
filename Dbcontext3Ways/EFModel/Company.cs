namespace Dbcontext3Ways.EFModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Table("acct_company")]
    class Company
    {
        public Company()
        {
            this.User = new HashSet<User>();
        }

        [Column("acct_company_id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Column("name", Order = 100)]
        [Required]
        [Index("ux_acct_company_name", IsUnique = true)]
        [MaxLength(300)]
        [MinLength(3)]
        public string Name { get; set; }

        [Column("display_name", Order = 120)]
        [Required]
        [MaxLength(300)]
        public string DisplayName { get; set; }

        public virtual ICollection<User> User { get; set; }
    }
}
