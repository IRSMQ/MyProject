using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Test26.Models;

namespace Test26.Context;

public partial class ProjectManagementSystemContext : DbContext
{
    public ProjectManagementSystemContext()
    {
    }

    public ProjectManagementSystemContext(DbContextOptions<ProjectManagementSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChangeLog> ChangeLogs { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Priority> Priorities { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<TaskManagement> TaskManagements { get; set; }

    public virtual DbSet<TaskUser> TaskUsers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserPermission> UserPermissions { get; set; }

    public virtual DbSet<UserProject> UserProjects { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=localhost\\MSSQLSERVER01;Initial Catalog=ProjectManagementSystem;Integrated Security=True;Pooling=False;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Name=vscode-mssql;Application Intent=ReadWrite;Command Timeout=30");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChangeLog>(entity =>
        {
            entity.HasKey(e => e.ChangeLogId).HasName("PK_ChangeLogs");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.ColumnName).HasMaxLength(40);
            entity.Property(e => e.NewValue).HasMaxLength(100);
            entity.Property(e => e.OldValue).HasMaxLength(100);
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_ChangeLogs_SoftDelete");

            entity.HasOne(d => d.Log).WithMany()
                .HasForeignKey(d => d.LogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChangeLogs_Log");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK_Event");

            entity.ToTable("Log");

            entity.Property(e => e.LogId)
                .HasDefaultValueSql("(newid())", "DF_Event_EventId")
                .HasColumnName("LogID");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())", "DF_Event_Date")
                .HasColumnType("datetime");
            entity.Property(e => e.LogType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RelatedId).HasColumnName("RelatedID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_Log_SoftDelete");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Logs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Event_User");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permission");

            entity.Property(e => e.PermissionId)
                .HasDefaultValueSql("(newid())", "DF_Permission_PermissionId")
                .HasColumnName("PermissionID");
            entity.Property(e => e.PermissionName).HasMaxLength(50);
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_Permission_SoftDelete");
        });

        modelBuilder.Entity<Priority>(entity =>
        {
            entity.ToTable("Priority");

            entity.Property(e => e.PriorityId)
                .ValueGeneratedNever()
                .HasColumnName("PriorityID");
            entity.Property(e => e.PriorityName).HasMaxLength(50);
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_Priority_SoftDelete");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Project");

            entity.Property(e => e.ProjectId)
                .HasDefaultValueSql("(newid())", "DF_Project_ProjectId")
                .HasColumnName("ProjectID");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())", "DF_Project_CreationDate")
                .HasColumnType("datetime");
            entity.Property(e => e.Desc).HasMaxLength(500);
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("endDate");
            entity.Property(e => e.ManagerId).HasColumnName("ManagerID");
            entity.Property(e => e.PriorityId).HasColumnName("PriorityID");
            entity.Property(e => e.ProjectName)
                .HasMaxLength(50)
                .HasDefaultValue("Test", "DF_Project_ProjectName");
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_Project_SoftDelete");
            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("(getdate())", "DF_Project_StartDate")
                .HasColumnType("datetime");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");

            entity.HasOne(d => d.Manager).WithMany(p => p.Projects)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK_Project_User");

            entity.HasOne(d => d.Priority).WithMany(p => p.Projects)
                .HasForeignKey(d => d.PriorityId)
                .HasConstraintName("FK_Project_Priority");

            entity.HasOne(d => d.Status).WithMany(p => p.Projects)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK_Project_Status");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.Property(e => e.RoleId)
                .ValueGeneratedNever()
                .HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_Role_SoftDelete");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("RolePermission");

            entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Rpid)
                 .HasDefaultValueSql("(newid())", "DF_RolePermission_RPID")
                 .HasColumnName("RPID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_RolePermission_SoftDelete");

            entity.HasOne(d => d.Permission).WithMany()
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermission_Permission");

            entity.HasOne(d => d.Role).WithMany()
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermission_Role");
        });

        // modelBuilder.Entity<RolePermission>(entity =>
        // {
        //     entity
        //         .HasNoKey()
        //         .ToTable("RolePermission");

        //     entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
        //     entity.Property(e => e.RoleId).HasColumnName("RoleID");
        //     entity.Property(e => e.Rpid)
        //         .HasDefaultValueSql("(newid())", "DF_RolePermission_RPID")
        //         .HasColumnName("RPID");
        // });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK_TaskStatus");

            entity.ToTable("Status");

            entity.Property(e => e.StatusId)
                .HasDefaultValueSql("(newid())", "DF_Status_StatusId")
                .HasColumnName("StatusID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_Status_SoftDelete");
            entity.Property(e => e.StatusName).HasMaxLength(100);
        });

        modelBuilder.Entity<TaskManagement>(entity =>
        {
            entity.ToTable("TaskManagement");

            entity.Property(e => e.TaskManagementId)
                .HasDefaultValueSql("(newid())", "DF_TaskManagement_TaskManagementId")
                .HasColumnName("TaskManagementID");
            entity.Property(e => e.CompletionDate).HasColumnType("datetime");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())", "DF_TaskManagement_CreationDate")
                .HasColumnType("datetime");
            entity.Property(e => e.Desc).HasMaxLength(500);
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.ParentId).HasColumnName("ParentID");
            entity.Property(e => e.PriorityId).HasColumnName("PriorityID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_TaskManagement_SoftDelete");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.Title).HasMaxLength(50);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_TaskManagement_TaskManagement");

            entity.HasOne(d => d.Priority).WithMany(p => p.TaskManagements)
                .HasForeignKey(d => d.PriorityId)
                .HasConstraintName("FK_TaskManagement_Priority");

            entity.HasOne(d => d.Project).WithMany(p => p.TaskManagements)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskManagement_Project");

            entity.HasOne(d => d.Status).WithMany(p => p.TaskManagements)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK_TaskManagement_Status");
        });

        modelBuilder.Entity<TaskUser>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TaskUser");

            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_TaskUser_SoftDelete");
            entity.Property(e => e.TaskId).HasColumnName("TaskID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Task).WithMany()
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskUser_TaskManagement");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskUser_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.UserId)
                .HasDefaultValueSql("(newid())", "DF_User_UserId")
                .HasColumnName("UserID");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())", "DF_User_CreationDate")
                .HasColumnType("datetime");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_User_SoftDelete");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserFullName).HasMaxLength(50);
            entity.Property(e => e.UserPassword)
                .HasMaxLength(100)
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

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UserPermission");

            entity.Property(e => e.Int).HasColumnName("int");
            entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_UserPermission_SoftDelete");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Permission).WithMany()
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPermission_Permission");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPermission_User");
        });

        modelBuilder.Entity<UserProject>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UserProject");

            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(true, "DF_UserProject_SoftDelete");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserProject_Project");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserProject_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
