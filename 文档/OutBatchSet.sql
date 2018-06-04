USE [AcWMSDB]
GO

/****** Object:  Table [dbo].[OutBatchSet]    Script Date: 07/02/2017 17:15:58 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[OutBatchSet](
	[OutBatchSetID] [bigint] IDENTITY(1,1) NOT NULL,
	[StoreHouseID] [bigint] NOT NULL,
	[StoreHouseLogicAreaID] [bigint] NOT NULL,
	[Batch] [nvarchar](200) NOT NULL,
	[Resever1] [nvarchar](50) NULL,
	[Resever2] [nvarchar](50) NULL,
	[Resever3] [nvarchar](50) NULL,
	[Resever4] [nvarchar](50) NULL,
	[Resever5] [nvarchar](50) NULL,
	[Resever6] [nvarchar](50) NULL,
	[Resever7] [nvarchar](50) NULL,
	[Resever8] [nvarchar](50) NULL,
 CONSTRAINT [PK_OutBatchSet] PRIMARY KEY CLUSTERED 
(
	[OutBatchSetID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'出库批次设置' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'OutBatchSetID'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'库房ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'StoreHouseID'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'库区ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'StoreHouseLogicAreaID'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备用1' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'Resever1'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备用2' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'Resever2'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备用3' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'Resever3'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备用4' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'Resever4'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备用5' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'Resever5'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备用6' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'Resever6'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备用7' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'Resever7'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'备用8' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OutBatchSet', @level2type=N'COLUMN',@level2name=N'Resever8'
GO

ALTER TABLE [dbo].[OutBatchSet]  WITH CHECK ADD  CONSTRAINT [FK_OutBatchSet_StoreHouse] FOREIGN KEY([StoreHouseID])
REFERENCES [dbo].[StoreHouse] ([StoreHouseID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[OutBatchSet] CHECK CONSTRAINT [FK_OutBatchSet_StoreHouse]
GO

ALTER TABLE [dbo].[OutBatchSet]  WITH CHECK ADD  CONSTRAINT [FK_OutBatchSet_StoreHouseLogicArea] FOREIGN KEY([StoreHouseLogicAreaID])
REFERENCES [dbo].[StoreHouseLogicArea] ([StoreHouseLogicAreaID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[OutBatchSet] CHECK CONSTRAINT [FK_OutBatchSet_StoreHouseLogicArea]
GO


