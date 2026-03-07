using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sovereign.Domain.Entities
{
    public class Contact
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid ContactId { get; set; }
    }
}
