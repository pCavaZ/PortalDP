using PortalDP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PortalDP.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Student> Students { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<ClassCancellation> ClassCancellations { get; set; }
        public DbSet<RecoveryClass> RecoveryClasses { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureStudent(modelBuilder);
            ConfigureSchedule(modelBuilder);
            ConfigureClassCancellation(modelBuilder);
            ConfigureRecoveryClass(modelBuilder);
            ConfigureTimeSlot(modelBuilder);

            SeedInitialData(modelBuilder);
        }

        private void ConfigureStudent(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.DNI)
                    .IsRequired()
                    .HasMaxLength(9);

                entity.Property(e => e.Email)
                    .HasMaxLength(255);

                entity.Property(e => e.Phone)
                    .HasMaxLength(20);

                entity.Property(e => e.IsActive);

                entity.Property(e => e.CreatedAt);

                entity.Property(e => e.UpdatedAt);

                // Indexes
                entity.HasIndex(e => e.DNI)
                    .IsUnique()
                    .HasDatabaseName("IX_Students_DNI");

                entity.HasIndex(e => e.IsActive)
                    .HasDatabaseName("IX_Students_IsActive");
            });
        }

        private void ConfigureSchedule(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Schedule>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.StudentId)
                    .IsRequired();

                entity.Property(e => e.DayOfWeek)
                    .IsRequired();

                entity.Property(e => e.StartTime)
                    .IsRequired();

                entity.Property(e => e.EndTime)
                    .IsRequired();

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Foreign key relationship
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Schedules)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Schedules_Students");

                // Indexes
                entity.HasIndex(e => new { e.StudentId, e.DayOfWeek, e.IsActive })
                    .HasDatabaseName("IX_Schedules_Student_Day_Active");

                entity.HasIndex(e => new { e.DayOfWeek, e.StartTime, e.EndTime })
                    .HasDatabaseName("IX_Schedules_TimeSlot");
            });
        }

        private void ConfigureClassCancellation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClassCancellation>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.StudentId)
                    .IsRequired();

                entity.Property(e => e.ClassDate)
                    .IsRequired();

                entity.Property(e => e.OriginalScheduleId)
                    .IsRequired();

                entity.Property(e => e.CancelledAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Reason)
                    .HasMaxLength(500);

                // Foreign key relationships
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.ClassCancellations)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ClassCancellations_Students");

                entity.HasOne(e => e.OriginalSchedule)
                    .WithMany(s => s.ClassCancellations)
                    .HasForeignKey(e => e.OriginalScheduleId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ClassCancellations_Schedules");

                // Indexes
                entity.HasIndex(e => new { e.StudentId, e.ClassDate })
                    .IsUnique()
                    .HasDatabaseName("IX_ClassCancellations_Student_Date");

                entity.HasIndex(e => e.ClassDate)
                    .HasDatabaseName("IX_ClassCancellations_Date");
            });
        }

        private void ConfigureRecoveryClass(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RecoveryClass>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.StudentId)
                    .IsRequired();

                entity.Property(e => e.ClassDate)
                    .IsRequired();

                entity.Property(e => e.StartTime)
                    .IsRequired();

                entity.Property(e => e.EndTime)
                    .IsRequired();

                entity.Property(e => e.OriginalCancellationId)
                    .IsRequired();

                entity.Property(e => e.BookedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Foreign key relationships
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.RecoveryClasses)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_RecoveryClasses_Students");

                entity.HasOne(e => e.OriginalCancellation)
                    .WithMany(c => c.RecoveryClasses)
                    .HasForeignKey(e => e.OriginalCancellationId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_RecoveryClasses_ClassCancellations");

                // Indexes
                entity.HasIndex(e => new { e.StudentId, e.ClassDate })
                    .HasDatabaseName("IX_RecoveryClasses_Student_Date");

                entity.HasIndex(e => new { e.ClassDate, e.StartTime, e.EndTime })
                    .HasDatabaseName("IX_RecoveryClasses_TimeSlot");

                entity.HasIndex(e => e.OriginalCancellationId)
                    .IsUnique()
                    .HasDatabaseName("IX_RecoveryClasses_OriginalCancellation");
            });
        }

        private void ConfigureTimeSlot(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TimeSlot>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.DayOfWeek)
                    .IsRequired();

                entity.Property(e => e.StartTime)
                    .IsRequired();

                entity.Property(e => e.EndTime)
                    .IsRequired();

                entity.Property(e => e.MaxCapacity)
                    .HasDefaultValue(10);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                // Indexes
                entity.HasIndex(e => new { e.DayOfWeek, e.StartTime, e.EndTime })
                    .IsUnique()
                    .HasDatabaseName("IX_TimeSlots_DayTime");

                entity.HasIndex(e => e.IsActive)
                    .HasDatabaseName("IX_TimeSlots_IsActive");
            });
        }

        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed TimeSlots - Horarios de la academia
            var timeSlots = new List<TimeSlot>();
            var timeSlotId = 1;

            // Lunes a Viernes (1-5), 4 turnos por día
            for (int dayOfWeek = 1; dayOfWeek <= 5; dayOfWeek++)
            {
                var dailySlots = new[]
                {
                    new { Start = new TimeSpan(10, 0, 0), End = new TimeSpan(12, 0, 0) }, // 10:00-12:00
                    new { Start = new TimeSpan(12, 0, 0), End = new TimeSpan(14, 0, 0) }, // 12:00-14:00
                    new { Start = new TimeSpan(16, 0, 0), End = new TimeSpan(18, 0, 0) }, // 16:00-18:00
                    new { Start = new TimeSpan(18, 0, 0), End = new TimeSpan(20, 0, 0) }  // 18:00-20:00
                };

                foreach (var slot in dailySlots)
                {
                    timeSlots.Add(new TimeSlot
                    {
                        Id = timeSlotId++,
                        DayOfWeek = dayOfWeek,
                        StartTime = slot.Start,
                        EndTime = slot.End,
                        MaxCapacity = 10,
                        IsActive = true
                    });
                }
            }

            modelBuilder.Entity<TimeSlot>().HasData(timeSlots);

            // Seed algunos estudiantes de ejemplo
            var students = new List<Student>
            {
                new Student
                {
                    Id = 1,
                    Name = "María García López",
                    DNI = "12345678A",
                    Email = "maria.garcia@email.com",
                    Phone = "666123456",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Student
                {
                    Id = 2,
                    Name = "Ana López Martín",
                    DNI = "87654321B",
                    Email = "ana.lopez@email.com",
                    Phone = "666654321",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Student
                {
                    Id = 3,
                    Name = "Carmen Ruiz Sánchez",
                    DNI = "11223344C",
                    Email = "carmen.ruiz@email.com",
                    Phone = "666112233",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Student
                {
                    Id = 4,
                    Name = "Marta Sánchez Rodríguez",
                    DNI = "55667788D",
                    Email = "marta.sanchez@email.com",
                    Phone = "666556677",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Student
                {
                    Id = 5,
                    Name = "Rosa Martín González",
                    DNI = "99887766E",
                    Email = "rosa.martin@email.com",
                    Phone = "666998877",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            modelBuilder.Entity<Student>().HasData(students);

            // Seed horarios de ejemplo
            var schedules = new List<Schedule>
            {
                new Schedule { Id = 1, StudentId = 1, DayOfWeek = 1, StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(14, 0, 0), IsActive = true, CreatedAt = DateTime.UtcNow }, // María - Lunes 12-14
                new Schedule { Id = 2, StudentId = 2, DayOfWeek = 2, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(12, 0, 0), IsActive = true, CreatedAt = DateTime.UtcNow }, // Ana - Martes 10-12
                new Schedule { Id = 3, StudentId = 3, DayOfWeek = 3, StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(18, 0, 0), IsActive = true, CreatedAt = DateTime.UtcNow }, // Carmen - Miércoles 16-18
                new Schedule { Id = 4, StudentId = 4, DayOfWeek = 4, StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(14, 0, 0), IsActive = true, CreatedAt = DateTime.UtcNow }, // Marta - Jueves 12-14
                new Schedule { Id = 5, StudentId = 5, DayOfWeek = 5, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(12, 0, 0), IsActive = true, CreatedAt = DateTime.UtcNow }  // Rosa - Viernes 10-12
            };

            modelBuilder.Entity<Schedule>().HasData(schedules);
        }

        // Override SaveChangesAsync para actualizar automáticamente UpdatedAt
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Student && (e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                if (entry.Entity is Student student)
                {
                    student.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}