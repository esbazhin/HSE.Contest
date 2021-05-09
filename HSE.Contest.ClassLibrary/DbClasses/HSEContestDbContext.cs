using HSE.Contest.ClassLibrary.DbClasses.Administration;
using HSE.Contest.ClassLibrary.DbClasses.Files;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HSE.Contest.ClassLibrary.DbClasses
{
    public class HSEContestDbContext : IdentityDbContext<User>
    {
        public DbSet<StudentTask> StudentTasks { get; set; }
        public DbSet<StudentResult> StudentResults { get; set; }
        public DbSet<DbFileInfo> Files { get; set; }
        public DbSet<Solution> Solutions { get; set; }
        public DbSet<CompilationResult> CompilationResults { get; set; }
        public DbSet<TestingResult> TestingResults { get; set; }
        public DbSet<TaskTest> TaskTests { get; set; }
        public DbSet<CodeStyleFiles> CodeStyleFiles { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }

        public DbSet<PlagiarismCheck> PlagiarismChecks { get; set; }
        public DbSet<PlagiarismResult> PlagiarismResults { get; set; }


        public HSEContestDbContext(DbContextOptions<HSEContestDbContext> options)
            : base(options)
        {
            //Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Solution>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<Solution>()
                .HasOne(t => t.File);

            modelBuilder.Entity<Solution>()
                .HasOne(t => t.Student);                

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

            modelBuilder.Entity<StudentResult>()
                .HasKey(t => new { t.StudentId, t.TaskId });

            modelBuilder.Entity<StudentResult>()
                .HasOne(sc => sc.Student)
                .WithMany(s => s.Results)
                .HasForeignKey(sc => sc.StudentId);

            modelBuilder.Entity<StudentResult>()
                .HasOne(sc => sc.Task)
                .WithMany(c => c.Results)
                .HasForeignKey(sc => sc.TaskId);

            modelBuilder.Entity<PlagiarismCheck>()
                .HasKey(t => t.TaskId);

            modelBuilder.Entity<PlagiarismResult>()
                .HasKey(t => new { t.SolutionId1, t.SolutionId2 });
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
