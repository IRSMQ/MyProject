using Test26.Models;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Test26.Data;

public partial class ProjectManagementSystemContext : DbContext
{
    public ProjectManagementSystemContext()
    {
    }
    public ProjectManagementSystemContext(DbContextOptions<ProjectManagementSystemContext> options)
        : base(options)
    {
    }
    public virtual DbSet<Event> Events { get; set; }
    public virtual DbSet<EventType> EventTypes { get; set; }
    public virtual DbSet<RelatedTable> RelatedTables { get; set; }
    public virtual DbSet<Permission> Permissions { get; set; }
    public virtual DbSet<Priority> Priorities { get; set; }
    public virtual DbSet<Project> Projects { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<RolePermission> RolePermissions { get; set; }
    public virtual DbSet<Status> Statuses { get; set; }
    public virtual DbSet<TaskManagement> TaskManagements { get; set; }
    public virtual DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Event");

            entity.Property(e => e.EventId)
                .HasDefaultValueSql("(newid())", "DF_Event_EventId")
                .HasColumnName("EventID");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())", "DF_Event_Date")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EventTypeId).HasColumnName("EventTypeID");
            entity.Property(e => e.RelatedId).HasColumnName("RelatedID");
            entity.Property(e => e.RelatedTableId).HasColumnName("RelatedTableID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.EventType).WithMany(p => p.Events)
                .HasForeignKey(d => d.EventTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Event_EventType");

            entity.HasOne(d => d.RelatedTable).WithMany(p => p.Events)
                .HasForeignKey(d => d.RelatedTableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Event_RelatedTable");
        });
        
        modelBuilder.Entity<EventType>(entity =>
        {
            entity.ToTable("EventType");

            entity.Property(e => e.EventTypeId)
                .ValueGeneratedNever()
                .HasColumnName("EventTypeID");
            entity.Property(e => e.EventTypeName).HasMaxLength(30);
        });
        
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permission");

            entity.Property(e => e.PermissionId).HasDefaultValueSql("(newid())", "DF_Permission_PermissionId");
            entity.Property(e => e.PermissionName).HasMaxLength(50);
        });

        modelBuilder.Entity<Priority>(entity =>
        {
            entity.ToTable("Priority");

            entity.Property(e => e.PriorityId).ValueGeneratedNever();
            entity.Property(e => e.PriorityName).HasMaxLength(50);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Project");

            entity.Property(e => e.ProjectId).HasDefaultValueSql("(newid())", "DF_Project_ProjectId");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())", "DF_Project_CreationDate")
                .HasColumnType("datetime");
            entity.Property(e => e.Desc).HasMaxLength(500);
            entity.Property(e => e.DueDate)
                .HasColumnType("datetime")
                .HasColumnName("dueDate");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("endDate");
            entity.Property(e => e.ProjectName)
                .HasMaxLength(50)
                .HasDefaultValue("Test", "DF_Project_ProjectName");
            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("(getdate())", "DF_Project_StartDate")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Manager).WithMany()
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_User");

            entity.HasOne(d => d.Priority)
                .WithMany(p => p.Projects)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_Priority");

            entity.HasOne(d => d.Status)
                .WithMany(p => p.Projects)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_Status");
            
        });

        modelBuilder.Entity<RelatedTable>(entity =>
        {
            entity.ToTable("RelatedTable");

            entity.Property(e => e.RelatedTableId)
                .ValueGeneratedNever()
                .HasColumnName("RelatedTableID");
            entity.Property(e => e.RelatedTableName).HasMaxLength(20);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.Property(e => e.RoleId).ValueGeneratedNever();
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new {rp.RoleId, rp.PermissionId});
                //.HasNoKey()
            entity.ToTable("RolePermission");

            entity.HasOne(d => d.Permission).WithMany()
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermission_Permission");

            entity.HasOne(d => d.Role).WithMany()
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermission_Role");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK_TaskStatus");

            entity.ToTable("Status");

            entity.Property(e => e.StatusId).ValueGeneratedNever();
            entity.Property(e => e.StatusName).HasMaxLength(100);
        });

        modelBuilder.Entity<TaskManagement>((Action<Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TaskManagement>>)(entity =>
        {
            entity.ToTable("TaskManagement");

            entity.Property(e => e.TaskManagementId).HasDefaultValueSql("(newid())", "DF_TaskManagement_TaskManagementId");
            entity.Property(e => e.CompletionDate).HasColumnType("datetime");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())", "DF_TaskManagement_CreationDate")
                .HasColumnType("datetime");
            entity.Property((System.Linq.Expressions.Expression<Func<TaskManagement, string?>>)(e => e.Desc)).HasMaxLength(500);
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(50);

            entity.HasOne(d => d.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskManagment_Parent");

            entity.HasOne(d => d.Priority).WithMany((System.Linq.Expressions.Expression<Func<Priority, IEnumerable<TaskManagement>?>>?)(p => p.TaskManagements))
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskManagement_Priority");

            /*
            entity.HasOne(d => d.Project)
                .WithMany((System.Linq.Expressions.Expression<Func<Priority, IEnumerable<TaskManagement>?>>?)(p => p.TaskManagements))
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskManagement_Project");
            */
            entity.HasOne(d => d.Project)
                .WithMany(p => p.TaskManagements)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskManagement_Project");


            entity.HasOne(d => d.Status).WithMany(p => p.TaskManagements)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskManagement_Status");

            entity.HasOne(d => d.User).WithMany(p => p.TaskManagements)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_TaskManagement_User");
        }));

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())", "DF_User_UserId");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())", "DF_User_CreationDate")
                .HasColumnType("datetime");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserFullName).HasMaxLength(50);
            entity.Property(e => e.UserPassword)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UserPhone)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_User_Role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
