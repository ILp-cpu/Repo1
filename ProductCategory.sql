CREATE TABLE [dbo].[LinkTable](
	[ProductId] [int] NOT NULL,
	[CategoryId] [int] NULL
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[LinkTable]  WITH CHECK ADD  CONSTRAINT [FK_LinkTable_Category] FOREIGN KEY([CategoryId])
REFERENCES [dbo].[Category] ([Id])
GO

ALTER TABLE [dbo].[LinkTable] CHECK CONSTRAINT [FK_LinkTable_Category]
GO

ALTER TABLE [dbo].[LinkTable]  WITH CHECK ADD  CONSTRAINT [FK_LinkTable_Product] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
GO

ALTER TABLE [dbo].[LinkTable] CHECK CONSTRAINT [FK_LinkTable_Product]


select pr.Name as ProductName, c.Name as CategoryName from 
Product pr left join LinkTable lt on pr.Id = lt.ProductId
left join Category c on lt.CategoryId=c.Id