Feature: ParsingS7VariableName
	
@mytag
Scenario: Parsing variable name for bool
	Given I have an Parser
	And I have the following variables
	| VarName          |
	| DB13.DBX3.1      |
	| Db403.X5.2       |
	| DB55DBX23.6      |
	| DB1.S255         |
	| DB1.S255.20      |
	| DB5.String887.20 |
	| DB506.B216       |
	| DB506.DBB216.5   |
	| DB506.D216       |
	| DB506.DINT216    |
	| DB506.INT216     |
	| DB506.DBW216     |
	| DB506.DUL216     |
	| DB506.DULINT216  |
	| DB506.DULONG216  |
	When I parse the var name
	Then the result should be
	| Operand | DbNr | Start | Length | Bit | Type     |
	| Db      | 13   | 3     | 1      | 1   | Bit      |
	| Db      | 403  | 5     | 1      | 2   | Bit      |
	| Db      | 55   | 23    | 1      | 6   | Bit      |
	| Db      | 1    | 255   | 0      | 0   | String   |
	| Db      | 1    | 255   | 20     | 0   | String   |
	| Db      | 5    | 887   | 20     | 0   | String   |
	| Db      | 506  | 216   | 1      | 0   | Byte     |
	| Db      | 506  | 216   | 5      | 0   | Byte     |
	| Db      | 506  | 216   | 4      | 0   | Double   |
	| Db      | 506  | 216   | 4      | 0   | DInteger |
	| Db      | 506  | 216   | 2      | 0   | Integer  |
	| Db      | 506  | 216   | 2      | 0   | Integer  |
	| Db      | 506  | 216   | 8      | 0   | ULong    |
	| Db      | 506  | 216   | 8      | 0   | ULong    |
	| Db      | 506  | 216   | 8      | 0   | ULong    |
