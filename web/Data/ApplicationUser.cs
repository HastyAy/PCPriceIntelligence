using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace web.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public ICollection<PCBuild> PCBuilds { get; set; } = new List<PCBuild>();
    }

}
