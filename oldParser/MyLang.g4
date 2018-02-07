grammar MyLang;

tokens
{
	NEWLINES,
	COMMENTS
}

@parser::members
{
	protected const int EOF = Eof;
}

@lexer::members
{
	protected const int EOF			= Eof;
	protected const int HIDDEN		= Hidden;
	protected const int NEWLINES	= Hidden;
	protected const int COMMENTS	= Hidden;
}

scope_OP:	'::';

post_OP:	'++' |	'--';
pre_OP:		'++' |	'--' |	'+'  |	'-'  |	'!'  |	'~'  | '*' | '&';

assign_OP:	'='  |	'*=' |	'/=' |	'%=' |	'+=' |	'-=' |	'<<=' | '>>=' |	'&=' |	'^=' |	'|=';
mult_OP:			'*'  |	'/'  |	'%';
add_OP:										'+' |	'-';
shift_OP:													'<<' |	'>''>';
bitand_OP:																	'&';
bitxor_OP:																			'^';
bitor_OP:																					'|';

and_OP:																		'&&';
or_OP:																						'||';

mem_OP:		'.'  | '->';
mptr_OP:	'.*' | '->*';

order_OP:	'<' |'<='|'>'|'>=';
equal_OP:	'=='|'!=';

//cast_MOD:	'c'|'s'|'d'|'r'|;

preOpExpr	:	pre_OP			expr;
castExpr	:	'(' anyType ')'	expr;
sizeofExpr	:	'sizeof'		expr
			|	'sizeof'	'('	expr ')';
newExpr		:	'new' anyType? ('(' exprs ')')?;
deleteExpr	:	'delete' (ary='['']')? expr;

expr
	:	expr	scope_OP		expr		# Tier1
	|	expr	(	post_OP
				|	'['			expr	']'
				|	'('			exprs?	')'
				|	mem_OP	ID
				)							# Tier2
	|	<assoc=right>
		(	preOpExpr
		|	castExpr
		|	sizeofExpr
		|	newExpr
		|	deleteExpr
		)									# Tier3
	|	expr	mptr_OP			expr		# Tier4
	|	expr	mult_OP			expr		# Tier5
	|	expr	add_OP			expr		# Tier6
	|	expr	shift_OP		expr		# Tier7
	|	expr	order_OP		expr		# Tier8
	|	expr	equal_OP		expr		# Tier9
	|	expr	bitand_OP		expr		# Tier10
	|	expr	bitxor_OP		expr		# Tier11
	|	expr	bitor_OP		expr		# Tier12
	|	expr	and_OP			expr		# Tier13
	|	expr	or_OP			expr		# Tier14
	|	<assoc=right>
		expr	( assign_OP
				| '?' expr ':')	expr		# Tier15
	|			'throw'			expr		# Tier16
	|	expr	','				expr		# Tier17
	|	ID	tt_exp							# Tier100
	|	idOrLit								# Tier104
	|	'('		expr	')'					# ParenExpr
	|	AUTOINDEX							# Tier200
	;

exprs
	:	expr (',' expr)*
	;

tt_exp
	:	'<' exprs '>'
	;


prog:	toplevel+;

toplevel	: DBL_LBRACK attr+=attrib (COMMA attr+=attrib)* DBL_RBRACK							# Attributes
			| NS	ID (SCOPE ID)*	SEMI														# Namespace
			| NS	ID (SCOPE ID)*	LCURLY	toplevel+	RCURLY									# Namespace
			| CLASS	 idTplType	LCURLY	class_expr*	RCURLY										# ClassDecl
			| STRUCT idTplType	LCURLY	class_expr*	RCURLY										# StructDecl
			| UNION	 idTplType	LCURLY	class_expr*	RCURLY										# UnionDecl
			| ENUM	ID			LCURLY	id_opt_value (COMMA id_opt_value)* COMMA? RCURLY		# EnumDecl
			| FUNC	funcmeth_decl																# FunctionDecl
			| expr SEMI																			# rest
			;

attrib	: 'poly'									# AttrPoly
		| 'pod' LPAREN ( 'force'|'permit') RPAREN	# AttrPOD
		;

id_opt_value: ID (ASSIGN expr)?;

class_expr	: 'static'	LCURLY class_extened_expr* RCURLY	# StaticDecl
			| 'ctor'	ctor_decl							# CtorDecl
			| class_extened_expr							# ClassExtendedDecl
			;

class_extened_expr	: FIELDS	LCURLY (field_expr		SEMI)* RCURLY
					| FIELD				field_expr		SEMI
					| PROP				field_expr		SEMI
					| METH				funcmeth_decl
					;

field_expr	: anyType id_opt_value (COMMA id_opt_value)*	# FieldDecl
			;

idOrLit		: arg=(ID|INTEGER_LIT|FLOAT_LIT|STRING_LIT|CHAR_LIT);

parameter			: anyType ID;
parameters			: LPAREN ( parameter	(COMMA parameter)* )? RPAREN;
initializationList	: COLON ID argumentList (COMMA ID argumentList)*;
argumentList		: LPAREN ( idOrLit		(COMMA idOrLit  )* )? RPAREN;

//property_expr	: ID;
ctor_decl		: parameters initializationList? LCURLY statements RCURLY	# CtorDef;
funcmeth_decl	: (	anyType	ID parameters
				  |			ID parameters RARROW anyType
				  |			ID parameters
				  ) LCURLY statements RCURLY								# FuncMeth;

statements	: statement*;
statement	: 'return'	expr SEMI											# ReturnStmt
			| 'break'	SEMI												# BreakStmt
			| IF		LPAREN expr RPAREN statement ( 'else' statement )?	# IfStmt
			| FOR		LPAREN expr ';' expr ';' expr RPAREN statement ( 'nothing' statement )?	# ForStmt
			| expr TIMES			statement								# TimesStmt
			| expr '..' expr		statement								# EachStmt
			| VAR		field_expr SEMI										# VariableDecl
			| expr SEMI														# ExpressionStmt
			| LCURLY statements RCURLY										# BlockStmt
			;

anyType	: typeQualifier*	( basicType | advancedType )	typePtr*;

typePtr	: ptr=( AT_BANG | AT_QUEST | AT_PLUS | DBL_AMP | AMP | STAR )		typeQualifier*
		| ary=( AT_LBRACK | LBRACK ) content=( ID | INTEGER_LIT )? RBRACK	typeQualifier*
		;

typeQualifier	: qual=(CONST  | VOLATILE | MUTABLE);
signQualifier	: qual=(SIGNED | UNSIGNED);

advancedType	: idTplType (SCOPE idTplType)*;
idTplType		: name=ID ('<' tpl=anyTypeOrConstCS '>')?;

anyTypeOrConstCS: anyTypeOrConst (COMMA anyTypeOrConst)*
				;

anyTypeOrConst	: anyType
				| INTEGER_LIT
				;

basicType	: VOID
			| BOOL
			| characterType
			| floatingType
			| signQualifier? integerType
			;

characterType: chr=(CHAR | CHAR16 | CHAR32 | WCHAR);

integerType	: CHAR
			| SHORT
			| LONG LONG INT?
			| LONG INT?
			| INT
			| signQualifier
			;

floatingType: FLOAT
			| LONG DOUBLE
			| DOUBLE
			;

anyTypeCS	: any_types+=anyType (COMMA any_types+=anyType)*
			;

expr_old
	:		LPAREN  anyType RPAREN	expr	# StaticCastExpr
	| expr	op=(DBL_MINUS|DBL_PLUS)			# PostCrementExpr
	| expr	LBRACK	expr RBRACK				# IndexExpr
	| ID	LPAREN	expr RPAREN				# FuncCallExpr
	| expr	POINT					expr	# ElementSelectExpr
	|<assoc=right> op=(DBL_MINUS|DBL_PLUS) expr	# PreCrementExpr
	| expr	STAR STAR				expr	# Pow
	|		SQRT					expr	# Sqrt
	| expr	op=(STAR|SLASH|MOD)		expr	# MulDivMod
	| expr	op=(PLUS|MINUS)			expr	# AddSub
	| expr	op=LSHIFT				expr	# LShift
	| expr	('>''>' )				expr	# RShift
	| expr	EQ						expr	# Equal
	| expr	NEQ						expr	# NotEqual
	| expr	LTEQ					expr	# LessEqualThan
	| expr	GTEQ					expr	# GreaterEqualThan
	| expr	LT						expr	# LessThan
	| expr	GT						expr	# GreaterThan
	| expr	ASSIGN					expr	# Assign
	| '-' expr								# UnaryMinus
	|		LPAREN expr RPAREN				# ParensExpr
	| 'nop'									# NopExpr
	| idOrLit								# IdOrLitExpr
	;

comment	: COMMENT
		;

/*
 * Lexer Rules
 */
COMMENT		: ( '//' ~('\r'|'\n')*
			  | '/*' .*? '*/'
			  )	-> channel(COMMENTS);

STRING_LIT	: '"' (STR_ESC | ~('\\' | '"'  | '\r' | '\n'))* '"';
CHAR_LIT	: '\'' (CH_ESC | ~('\\' | '\'' | '\r' | '\n')) '\'';
fragment STR_ESC: '\\' ('\\' | '"' | 't' | 'n' | 'r');
fragment CH_ESC:  '\\' ('\\' | '\'' | 't' | 'n' | 'r');

TRP_POINT	: '...';
DBL_POINT	: '..';
DBL_LBRACK	: '[[';
DBL_RBRACK	: ']]';
DBL_AMP		: '&&';
//DBL_STAR	: '**';
DBL_PLUS	: '++';
DBL_MINUS	: '--';
RARROW		: '->';
LSHIFT		: '<<';
//RSHIFT		: '>>';
SCOPE		: '::';
AT_BANG		: '@!';
AT_QUEST	: '@?';
AT_PLUS		: '@+';
AT_LBRACK	: '@[';
LBRACK		: '[';
RBRACK		: ']';
LCURLY		: '{';
RCURLY		: '}';
LPAREN		: '(';
RPAREN		: ')';
AUTOINDEX	: '#' DIGIT;
AT			: '@';
AMP			: '&';
STAR		: '*';
SLASH		: '/';
MOD			: '%';
PLUS		: '+';
MINUS		: '-';
SEMI		: ';';
COLON		: ':';
COMMA		: ',';
POINT		: '.';

EQ			: '==';
NEQ			: '!=';
LTEQ		: '<=';
GTEQ		: '>=';
LT			: '<';
GT			: '>';

ASSIGN		: '=';

UNSIGNED	: 'unsigned';
SIGNED		: 'signed';
VOID		: 'void';
BOOL		: 'bool';
CHAR		: 'char';
CHAR16		: 'char16_t';
CHAR32		: 'char32_t';
WCHAR		: 'wchar_t';
SHORT		: 'short';
INT			: 'int';
LONG		: 'long';
FLOAT		: 'float';
DOUBLE		: 'double';

NS			: 'namespace';
VOLATILE	: 'volatile';
CONST		: 'const';
MUTABLE		: 'mutable';
SQRT		: 'sqrt';
UNION		: 'union';
CLASS		: 'class';
STRUCT		: 'struct';
FUNC		: 'func';
METH		: 'meth';
ENUM		: 'enum';
PROP		: 'prop';
FIELDS		: 'fields';
FIELD		: 'field';
VAR			: 'var';
FOR			: 'for';
TIMES		: 'times';
IF			: 'if';
ID			: [a-zA-Z_][a-zA-Z0-9_]*;
FLOAT_LIT	:	(	DIGIT* '.' DIGIT+ ( [eE] [+-]? DIGIT+ )?
				|	DIGIT+ [eE] [+-]? DIGIT+ 
				) [lfLF]?;
INTEGER_LIT	: DIGIT+;
fragment DIGIT: [0-9];
NL			: ('\r'|'\n')+	-> channel(NEWLINES);
WS			: (' '|'\t')+	-> skip;// channel(HIDDEN);
