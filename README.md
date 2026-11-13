# app-dev
My first repository
StockSmart: A Sari-Sari Store Inventory, Sales, and Credit (Utang) Management System

Description:
StockSmart is a desktop application built with .NET Windows Forms and MongoDB. It helps small neighborhood stores manage their products, sales, and customer credit in one place.

How the system works:

Login (role-based): The Owner and the Cashier log in. The Owner can access all modules, while the Cashier can only access the sales module.
Product management (CRUD): Add, view, update, and delete products with name, category, price, stock, and reorder level.
Sales / POS (transaction processing): The cashier selects products and quantities and chooses cash or credit payment. Saving the sale records the transaction and automatically deducts the stock.
Customer credit (utang): Credit sales are linked to a customer record, and the customer’s balance goes down when they make a payment.
Filtering and searching: Products can be filtered by name, category, or low stock. Sales can be filtered by date range or customer.
Reports: Daily and monthly sales totals, best-selling products, low-stock alerts, and a list of customers with unpaid balances.

Problem it solves:
Many small stores still use notebooks to track stock, sales, and utang. This leads to computing errors, lost records, unnoticed stockouts, and forgotten debts.
