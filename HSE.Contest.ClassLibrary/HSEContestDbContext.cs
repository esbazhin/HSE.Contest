using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary
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

            //modelBuilder.Entity<UserRole>()
            //    .HasKey(t => new { t.UserId, t.RoleId });

            //modelBuilder.Entity<UserRole>()
            //    .HasOne(sc => sc.User)
            //    .WithMany(s => s.Roles)
            //    .HasForeignKey(sc => sc.UserId);

            //modelBuilder.Entity<UserRole>()
            //    .HasOne(sc => sc.Role)
            //    .WithMany(c => c.Users)
            //    .HasForeignKey(sc => sc.RoleId);

            //modelBuilder.Entity<UserGroup>()
            //    .HasKey(t => new { t.UserId, t.GroupId });

            //modelBuilder.Entity<UserGroup>()
            //    .HasOne(sc => sc.User)
            //    .WithMany(s => s.Groups)
            //    .HasForeignKey(sc => sc.UserId);

            //modelBuilder.Entity<UserGroup>()
            //    .HasOne(sc => sc.Group)
            //    .WithMany(c => c.Users)
            //    .HasForeignKey(sc => sc.GroupId);
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

    public class DbFileInfo
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; }
        [Column(name: "content", TypeName = "bytea")]
        public byte[] Content { get; set; }
    }

    public class Solution
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }

        [Column(name: "taskId")]
        public int TaskId { get; set; }
        public virtual StudentTask Task { get; set; }

        [Column(name: "studentId")]
        public int StudentId { get; set; }
        //public User Student { get; set; }

        [Column(name: "score")]
        public double Score { get; set; }

        [Column(name: "resultCode", TypeName = "integer")]
        public ResultCode ResultCode { get; set; }

        [Column(name: "fileId")]
        public int FileId { get; set; }
        public virtual DbFileInfo File { get; set; }

        [Column(name: "time", TypeName = "timestamptz")]
        public DateTime Time { get; set; }

        [Column(name: "frameworkType")]
        public string FrameworkType { get; set; }

        //[Column(name: "updateRulesFiles")]
        //public bool UpdateRulesFiles { get; set; }


        [Column(name: "compilationId")]
        public int? CompilationId { get; set; }

        //[Column(name: "reflectionId")]
        //public int? ReflectionId { get; set; }

        //[Column(name: "functionalId")]
        //public int? FunctionalId { get; set; }

        //[Column(name: "codeStyleId")]
        //public int? CodeStyleId { get; set; }
    }

    public class CompilationResult
    {
        [Key]
        [Column(name: "solutionId")]
        public int SolutionId { get; set; }

        [Column(name: "stOutput", TypeName = "text")]
        public string StOutput { get; set; }

        [Column(name: "stError", TypeName = "text")]
        public string StError { get; set; }

        [Column(name: "resultCode", TypeName = "integer")]
        public ResultCode ResultCode { get; set; }

        [Column(name: "didUpdateRules")]
        public bool DidUpdateRules { get; set; }

        [Column(name: "fileId")]
        public int? FileId { get; set; }
        public virtual DbFileInfo File { get; set; }
    }

    public class TestingResult
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }

        [Column(name: "solutionId")]
        public int SolutionId { get; set; }

        [Column(name: "score")]
        public double Score { get; set; }

        [Column(name: "comment")]
        public string Commentary { get; set; }

        [Column(name: "resultCode", TypeName = "integer")]
        public ResultCode ResultCode { get; set; }

        [Column(name: "testId")]
        public int TestId { get; set; }

        [Column(name: "data", TypeName = "json")]
        public string TestData { get; set; }
    }

    public class TaskTest
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }

        [Column(name: "taskId")]
        public int TaskId { get; set; }

        [Column(name: "testType")]
        public string TestType { get; set; }

        [Column(name: "weight")]
        public double Weight { get; set; }

        [Column(name: "block")]
        public bool Block { get; set; }

        [Column(name: "data", TypeName = "json")]
        public string TestData { get; set; }
    }

    public class CodeStyleFiles
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }

        [Column(name: "name")]
        public string Name { get; set; }

        [Column(name: "stylecopFile", TypeName = "bytea")]
        public byte[] StyleCopFile { get; set; }

        [Column(name: "rulesetFile", TypeName = "bytea")]
        public byte[] RulesetFile { get; set; }
    }
}
