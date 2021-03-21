﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Trader.Data;

namespace Trader.Data.Migrations
{
    [DbContext(typeof(TraderContext))]
    [Migration("20210320204643_Create")]
    partial class Create
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.4");

            modelBuilder.Entity("Trader.Data.OrderEntity", b =>
                {
                    b.Property<long>("OrderId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClientOrderId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<decimal>("CummulativeQuoteQuantity")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("ExecutedQuantity")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("IcebergQuantity")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsWorking")
                        .HasColumnType("INTEGER");

                    b.Property<long>("OrderListId")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("OriginalQuantity")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("OriginalQuoteOrderQuantity")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Price")
                        .HasColumnType("TEXT");

                    b.Property<int>("Side")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("StopPrice")
                        .HasColumnType("TEXT");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT");

                    b.Property<int>("TimeInForce")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("UpdateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("OrderId");

                    b.HasIndex("Symbol", "OrderId");

                    b.ToTable("Orders");
                });
#pragma warning restore 612, 618
        }
    }
}