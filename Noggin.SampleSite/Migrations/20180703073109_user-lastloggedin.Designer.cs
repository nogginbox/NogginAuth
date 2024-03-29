﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Noggin.SampleSite.Data;

namespace Noggin.SampleSite.Migrations;

[DbContext(typeof(SampleSimpleDbContext))]
[Migration("20180703073109_user-lastloggedin")]
partial class userlastloggedin
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "2.1.1-rtm-30846");

        modelBuilder.Entity("Noggin.SampleSite.Data.User", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd();

                b.Property<DateTime>("LastLoggedIn");

                b.Property<string>("Name");

                b.HasKey("Id");

                b.ToTable("Users");
            });

        modelBuilder.Entity("Noggin.SampleSite.Data.UserAuthAccount", b =>
            {
                b.Property<string>("Id")
                    .ValueGeneratedOnAdd()
                    .HasMaxLength(64);

                b.Property<string>("Provider")
                    .HasMaxLength(32);

                b.Property<int?>("UserId");

                b.Property<string>("UserName")
                    .HasMaxLength(32);

                b.HasKey("Id");

                b.HasIndex("UserId");

                b.ToTable("UserAuthAccount");
            });

        modelBuilder.Entity("Noggin.SampleSite.Data.UserAuthAccount", b =>
            {
                b.HasOne("Noggin.SampleSite.Data.User")
                    .WithMany("AuthAccounts")
                    .HasForeignKey("UserId");
            });
#pragma warning restore 612, 618
    }
}
