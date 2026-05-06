# .Net WPF Inventory & Billing System

A professional Windows desktop application built with C# and the .NET Framework, designed for inventory management and automated billing. This project demonstrates modern architectural patterns including **MVVM**, **Entity Framework Migrations**, and **Document Generation**.

## 🚀 Key Features
* **Dashboard Management:** Real-time overview of users and products.
* **Database Versioning:** Utilizes Entity Framework Migrations for schema evolution.
* **PDF Generation:** Integrated with **QuestPDF** for professional invoice and report creation.
* **Modern UI:** Built with WPF and XAML for a clean, responsive desktop experience.

## 🛠 Tech Stack
* **Language:** C#
* **Framework:** .NET Framework 4.7.2
* **UI:** WPF (Windows Presentation Foundation)
* **ORM:** Entity Framework 6
* **PDF Engine:** QuestPDF

## 📂 Project Structure
```text
WpfApp1/
├── Migrations/       # Database version control files
├── Model/            # Database entities (Users, Products, Orders)
├── Pages/            # XAML Views (Login, Dashboards, Home)
├── packages.config   # NuGet dependency list
└── AppDbContext.cs   # Data access layer
