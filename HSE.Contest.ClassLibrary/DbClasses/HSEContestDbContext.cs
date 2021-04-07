using HSE.Contest.ClassLibrary.DbClasses.Administration;
using HSE.Contest.ClassLibrary.DbClasses.Files;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using Microsoft.EntityFrameworkCore;

namespace HSE.Contest.ClassLibrary.DbClasses
{
    public class HSEContestDbContext : DbContext
    {
        public DbSet<StudentTask> StudentTasks { get; set; }
        public DbSet<DbFileInfo> Files { get; set; }
        public DbSet<Solution> Solutions { get; set; }
        public DbSet<CompilationResult> CompilationResults { get; set; }
        public DbSet<TestingResult> TestingResults { get; set; }
        public DbSet<TaskTest> TaskTests { get; set; }
        public DbSet<CodeStyleFiles> CodeStyleFiles { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }


        public HSEContestDbContext(DbContextOptions<HSEContestDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Solution>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<Solution>()
                .HasOne(t => t.File);

            modelBuilder.Entity<CompilationResult>()
                .HasKey(t => t.SolutionId);

            modelBuilder.Entity<CompilationResult>()
                .HasOne(t => t.File);

            modelBuilder.Entity<DbFileInfo>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<TaskTest>()
                .HasOne(sc => sc.Task)
                .WithMany(s => s.Tests)
                .HasForeignKey(sc => sc.TaskId);

            modelBuilder.Entity<UserRole>()
                .HasKey(t => new { t.UserId, t.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(sc => sc.User)
                .WithMany(s => s.Roles)
                .HasForeignKey(sc => sc.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(sc => sc.Role)
                .WithMany(c => c.Users)
                .HasForeignKey(sc => sc.RoleId);

            modelBuilder.Entity<UserGroup>()
                .HasKey(t => new { t.UserId, t.GroupId });

            modelBuilder.Entity<UserGroup>()
                .HasOne(sc => sc.User)
                .WithMany(s => s.Groups)
                .HasForeignKey(sc => sc.UserId);

            modelBuilder.Entity<UserGroup>()
                .HasOne(sc => sc.Group)
                .WithMany(c => c.Users)
                .HasForeignKey(sc => sc.GroupId);
        }

        public int UploadFile(string name, byte[] content)
        {
            var file = new DbFileInfo
            {
                Name = name,
                Content = content,
            };

            var x = Files.Add(file);
            var beforeState = x.State;
            int r = this.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == EntityState.Added && afterState == EntityState.Unchanged && r == 1;

            if (!ok)
            {
                return -1;
            }

            return file.Id;
        }
    }
}
