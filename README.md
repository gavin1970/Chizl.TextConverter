# Chizl.TextConverter

[![NuGet Version](https://img.shields.io/nuget/v/Chizl.TextConverter)](https://www.nuget.org/packages/Chizl.TextConverter)
[![License](https://img.shields.io/badge/license-MIT-green)](https://github.com/gavin1970/Chizl.TextConverter/blob/master/LICENSE.md)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.0%20%7C%202.1%20%7C%20Framework%204.8%20%7C%20.NET%208-purple)](https://dotnet.microsoft.com/)

A robust, high-performance .NET library for loading, validating, and converting text-based data files into strongly-typed `DataTable` objects. Seamlessly transform between multiple file formats with built-in validation, column-level type conversion, and comprehensive audit logging.

---

## 🎯 Key Features

- **Multi-Format Support**: Read and write Fixed Length, CSV, Tab-Delimited, Semicolon-Delimited, and Quote-Comma-Delimited files
- **Type-Safe Conversion**: Automatic conversion to 8 strongly-typed data types with decimal precision control
- **Built-in Validation**: Column-level validation with allowed value constraints and null-handling
- **Comprehensive Audit Logging**: Detailed success/error tracking with `AuditLog` for debugging and reporting
- **Flexible Column Definitions**: Define schemas with size constraints, data types, and validation rules
- **Zero Dependencies**: No external dependencies required
- **Cross-Platform**: Supports .NET Standard 2.0/2.1, .NET Framework 4.8, and .NET 8

---

## 📦 Installation

### NuGet Package Manager

Install-Package Chizl.TextConverter

### .NET CLI

dotnet add package Chizl.TextConverter

### Package Reference

&lt;PackageReference Include="Chizl.TextConverter" /&gt;
> **Note:** Omitting the version attribute automatically uses the latest stable release. To pin a specific version, use `Version="x.x.x"`.

---

## 🚀 Quick Start

### Loading a CSV File

```csharp
using Chizl.TextConverter; 
using System.Data;

// Define column schema 
LoadFile loadFile = new(@"C:\data\customers.csv", FileTypes.Comma_Delimited) 
	{ FirstRowIsHeader = true, TrimValues = true, 
		ColumnDefinitions = new() { 
			new ColumnDefinition("CustomerID", DataTypes.Int64), 
			new ColumnDefinition("Name", DataTypes.String, 50), 
			new ColumnDefinition("Email", DataTypes.String, 100),
			new ColumnDefinition("CreatedDate", DataTypes.DateTime),
			new ColumnDefinition("Balance", DataTypes.Decimal, decimalSize: 2),
			new ColumnDefinition("IsActive", DataTypes.Boolean) 
		} 
	};

	// Load and validate 
	if (loadFile.LoadToDataTable()) 
	{ 
		DataTable customers = loadFile.AsDataTable; 
		Console.WriteLine($"Loaded {customers.Rows.Count} customers successfully!");
	} 
	else 
	{ 
		// Handle validation errors 
		foreach (AuditLog log in loadFile.AuditLogs.Where(l => l.MessageType == MessageTypes.Error)) 
			Console.WriteLine($"[{log.ValidationType}] Line {log.Location}: {log.Message}"); 
	}
```

### Converting Between File Formats

```csharp
// Load from CSV 
LoadFile loadFile = new(@"C:\data\input.csv", FileTypes.Comma_Delimited) 
{ 
	FirstRowIsHeader = true, 
	ColumnDefinitions = GetColumnDefinitions() 
};

if (loadFile.LoadToDataTable()) 
{ 
	// Save as Fixed Length file 
	SaveFile saveFile = new(loadFile.AsDataTable, @"C:\data\output.txt", 
							FileTypes.Fixed_Length_Columns, fileByColDefOnly: true, 
							overwriteFile: true ) 
	{ 
		FirstRowIsHeader = true, 
		ColumnDefinitions = loadFile.ColumnDefinitions 
	};
	if (saveFile.Save())
		Console.WriteLine("File converted successfully!");
}
```

---

## 📋 Supported File Types

| File Type | Enum Value | Description |
|-----------|-----------|-------------|
| Fixed Length Columns | `FileTypes.Fixed_Length_Columns` | Mainframe-style fixed-width columns |
| CSV (Comma Delimited) | `FileTypes.Comma_Delimited` | Standard comma-separated values |
| Quote-Comma Delimited | `FileTypes.Quote_Comma_Delimited` | CSV with quoted values (`"value","value"`) |
| Tab Delimited | `FileTypes.Tab_Delimited` | Tab-separated values (TSV) |
| Semicolon Delimited | `FileTypes.Semicolon_Delimited` | Semicolon-separated values |

---

## 🔢 Supported Data Types

| DataType | .NET Type | Notes |
|----------|-----------|-------|
| `DataTypes.String` | `System.String` | Use `size` parameter for max length |
| `DataTypes.Int64` | `System.Int64` | Covers all integer types (Int16, Int32, Int64) |
| `DataTypes.Decimal` | `System.Decimal` | Use `decimalSize` for precision control |
| `DataTypes.DateTime` | `System.DateTime` | Standard date/time parsing |
| `DataTypes.Boolean` | `System.Boolean` | Accepts true/false values |
| `DataTypes.Guid` | `System.Guid` | UUID validation |
| `DataTypes.TimeSpan` | `System.TimeSpan` | Duration/time interval |
| `DataTypes.ByteArray` | `System.Byte[]` | Base64-encoded binary data |

> **Why Int64 instead of Int32?** Since this is a temporary conversion tool, `Int64` covers all integer scenarios. Similarly, `Decimal` covers floating-point values. Set `size` and `decimalSize` for precision control.

---

## 🔧 Advanced Usage

### Column Definitions with Validation

```csharp
var columnDefinitions = new List<ColumnDefinition> 
{ 
	// Required string field with max length 
	new ColumnDefinition("Status", DataTypes.String, 1) 
	{ 
		AllowedValues = new List<object> { "A", "I", "P" } 
		// Active, Inactive, Pending 
	},

	// Nullable integer field
	new ColumnDefinition("ManagerID", DataTypes.Int64) 
	{ 
		AllowDBNull = true 
	},

	// Decimal with 2 decimal places
	new ColumnDefinition("Price", DataTypes.Decimal, decimalSize: 2),

	// Fixed-width string (for Fixed Length files)
	new ColumnDefinition("AccountNumber", DataTypes.String, 10)
};
```

### Fixed Length Column Files
```csharp
// Example: HUD GSE Loan Limits format 
// https://apps.hud.gov/pub/chums/cy2024-gse-limits.txt 
LoadFile loadFile = new(@".\data\gse-limits.txt", FileTypes.Fixed_Length_Columns) 
{ 
	ColumnDefinitions = new() 
	{ 
		new ColumnDefinition("MSACode", DataTypes.Int64, 5), 
		new ColumnDefinition("MetroDivCode", DataTypes.Int64, 5), 
		new ColumnDefinition("MSAName", DataTypes.String, 50), 
		new ColumnDefinition("StateCode", DataTypes.String, 5), 
		new ColumnDefinition("LimitType", DataTypes.String, 1) 
		{ 
			AllowedValues = new List<object> { "S", "H" } // Standard/High Cost 
		}, 
		new ColumnDefinition("MedianPrice", DataTypes.Int64, 7), 
		new ColumnDefinition("LimitFor1Unit", DataTypes.Int64, 7) 
	} 
};

if (loadFile.LoadToDataTable()) 
	Console.WriteLine($"Loaded {loadFile.AsDataTable.Rows.Count} loan limits"); 
```

### Audit Logging and Error Handling
```csharp
LoadFile loadFile = new(@".\data\file.csv", FileTypes.Comma_Delimited);  // ... configure and load ...

// Filter errors only 
var errors = loadFile.AuditLogs.Where(log => log.MessageType == MessageTypes.Error).ToList();
foreach (var error in errors) 
{ 
	Console.WriteLine($"[{error.ValidationType}] Line {error.Location}"); 
	Console.WriteLine($"  Column: {error.Column.Name}"); 
	Console.WriteLine($"  Message: {error.Message}"); 
}

// Export audit log to file 
File.WriteAllLines(@".\audit.log", loadFile.AuditLogs.Select(l => $"{l.MessageType} | {l.ValidationType} | Line {l.Location} | {l.Message}"));
```

### Saving with Custom Options

```csharp
SaveFile saveFile = new(dataTable, @"C:\data\output.csv", FileTypes.Comma_Delimited) 
{ 
	FirstRowIsHeader = true, 
	TrimValues = true, 
	ColumnDefinitions = GetColumnDefinitions() // Optional: use same column definitions for formatting 
};
if (saveFile.Save())
	Console.WriteLine("File saved successfully!");

SaveFile saveFile = new(dataTable: myDataTable, 
						dstFile: @"C:\output\export.txt", 
						dstFileType: FileTypes.Quote_Comma_Delimited, 
						fileByColDefOnly: true,     // Use only defined columns (ignore extras) 
						overwriteFile: true,        // Replace existing file 
						createFolder: true)			// Auto-create directory structure 
						{ 
							FirstRowIsHeader = true, 
							TrimValues = true, 
							ColumnDefinitions = columnDefinitions 
						};

if (!saveFile.Save()) 
{ 
	// Check for save errors 
	var saveErrors = saveFile.AuditLogs.Where(l => l.MessageType == MessageTypes.Error);
	foreach (var error in saveErrors) 
		Console.WriteLine($"Error saving file: {error.Message}");
}
```

---

## 📚 API Reference

### LoadFile Class

| Property/Method | Description |
|----------------|-------------|
| `LoadFile(string, FileTypes)` | Constructor: Initialize with file path and type |
| `ColumnDefinitions` | Define column schema for validation |
| `FirstRowIsHeader` | Treat first row as column names |
| `TrimValues` | Auto-trim whitespace from values |
| `LoadToDataTable()` | Load and validate file into `AsDataTable` |
| `AsDataTable` | Access the loaded `DataTable` |
| `AuditLogs` | Review validation messages and errors |

### SaveFile Class

| Property/Method | Description |
|----------------|-------------|
| `SaveFile(DataTable, string, FileTypes, ...)` | Constructor: Initialize with source data |
| `FileByColDefOnly` | Save only columns in `ColumnDefinitions` |
| `Save()` | Write file to disk |
| `AuditLogs` | Review save operation messages |

### ColumnDefinition Class

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Column name |
| `DataType` | `DataTypes` | Target data type |
| `Size` | `int` | Max length (String) or fixed width |
| `DecimalSize` | `int` | Decimal places (-1 = no rounding) |
| `AllowedValues` | `List<object>` | Whitelist of valid values |
| `AllowDBNull` | `bool` | Allow null/empty values |

---

## 📖 FAQ

**Q: Can I load a file without column definitions?**  
A: No. `ColumnDefinitions` are required to ensure type safety and validation. This prevents data corruption and provides clear schema documentation.

**Q: Why does my nullable column still show `DBNull` instead of `null`?**  
A: `DataTable` uses `DBNull.Value` for null database values. Check with `dataRow[column] == DBNull.Value` or use extension methods to convert.

**Q: How do I handle binary data like images?**  
A: Use `DataTypes.ByteArray` with Base64 encoding. The library automatically encodes/decodes during save/load operations.

**Q: Can I validate values against a specific list?**  
A: Yes! Set `AllowedValues` on your `ColumnDefinition`:


---

## 🛠️ Requirements

- **.NET Standard 2.0+** - Compatible with .NET Core, .NET 5+, .NET Framework 4.6.1+
- **.NET Framework 4.8** - Full Framework support
- **.NET 8** - Latest .NET platform
- **C# 7.3+** - Language features

---

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes with clear messages
4. Push to your branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](https://github.com/gavin1970/Chizl.TextConverter/blob/master/Chizl.TextConverter/docs/LICENSE.md) file for details.

---

## 🔗 Links

- **Homepage**: [www.chizl.com](http://www.chizl.com/)
- **GitHub**: [github.com/gavin1970/Chizl.TextConverter](https://github.com/gavin1970/Chizl.TextConverter)
- **NuGet**: [nuget.org/packages/Chizl.TextConverter](https://www.nuget.org/packages/Chizl.TextConverter)
- **Issues**: [Report a bug or request a feature](https://github.com/gavin1970/Chizl.TextConverter/issues)

---

## 📝 Changelog

### ![Latest Version](https://img.shields.io/nuget/v/Chizl.TextConverter)
- Updated version to because of update into nuget.
- Updated README.md and moved from docs folder to root for Github.  
	- Visual Studio isn't showing the README.md in NuGet Solution window, but shows it under Package Details. 
	- `Still Doesn't show in NuGet Solution window, but at least shows in Package Details.`

### ![v6.2.13.1](https://img.shields.io/badge/nuget-v6.2.13.1-blue)
- Updated version to reflect new dates and reset assembly version to 6.0.0.0.
- Updated README.md

### ![v4.6.29](https://img.shields.io/badge/nuget-v4.6.29-blue)
- Original Upload (2024-06-29)
- Stable release
- Multi-target framework support
- Comprehensive audit logging
- Full format conversion capabilities

---

**Made with ❤️ by Chizl** | © 2026 chizl.com