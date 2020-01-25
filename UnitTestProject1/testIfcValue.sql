declare @tbl table (val IfcValue)
insert into @tbl
values 
(IfcValue::Parse('*')),
(IfcValue::Parse('273')),
(IfcValue::Parse('12.34')),
(IfcValue::Parse('''t''')),
(IfcValue::Parse('#11')),
(IfcValue::Parse('.U.')),
(IfcValue::Parse('($, *, 111, ''test'')')),
(IfcValue::Parse('$')),
(IfcValue::Parse('()'))

select dbo.ValuesToList(val), count(val) from @tbl
select val, val.ToString() from @tbl