parser grammar MyParser;

options { tokenVocab = MyLexer; }

/*
@parser::members
{
	protected const int EOF = Eof;
}
*/

postOP		:	'++' |	'--';
preOP		:	'++' |	'--' |	'+'  |	'-'  |	'!'  |	'~'  | '*' | '&';

assignOP	:	'='  |	'**='|	'*=' |	'/=' |	'%=' |	'+=' |	'-='
			|	'<<='| '>>=' |	'&=' |	'^=' |	'|=';
powOP		:			'*''*';
multOP		:					'*'  |	'/'  |	'%';
addOP		:											'+' |	'-';
shiftOP		: 	'<<' |	'>''>';
bitandOP	:					'&';
bitxorOP	:							'^';
bitorOP		:									'|';
andOP		:					'&&';
orOP		:									'||';

memOP		:	'.'  | '->';
memptrOP	:	'.*' | '->*';

orderOP		:	'<' |'<='|'>'|'>=';
equalOP		:	'=='|'!=';

comment		: COMMENT;

id			: ID;
anyId		: ID | AUTOINDEX | USCORE;
idOrLit		: ID | INTEGER_LIT | FLOAT_LIT | STRING_LIT | CHAR_LIT | BOOL_LIT | NUL;

characterType: CHAR | CODEPOINT | STRING;

floatingType: FLOAT | F80 | F64 | F32 | F16;

binaryType	: BYTE | B64 | B32 | B16 | B8;

integerType	: INT | UINT | ISIZE | USIZE
			| I64 | I32 | I16 | I8
			| U64 | U32 | U16 | U8
			;

basicType	: AUTO
			| VOID
			| BOOL
			| characterType
			| floatingType
			| binaryType
			| integerType
			;

attribBlock	: LBRACK attrib (COMMA attrib)* RBRACK;
attrib		: ID	(	'=' idOrLit
					|	'(' idOrLit (COMMA idOrLit)* ')'
					)?;

statementBlock: LCURLY statement* RCURLY;
statement	: RETURN	expr SEMI											# ReturnStmt
			| BREAK		SEMI												# BreakStmt
			| IF		LPAREN expr RPAREN statement ( ELSE statement )?	# IfStmt
			| FOR		LPAREN statement expr SEMI expr RPAREN statement ( ELSE statement )?	# ForStmt
			| expr TIMES ID?		statement								# TimesStmt
			| expr '..' expr		statement								# EachStmt
			| VAR		typedIdExpr SEMI										# VariableDecl
			| CONST		typedIdExpr SEMI										# VariableDecl
			//| (expr		assign_OP)+ expr	SEMI							# AssignmentStmt
			| USING		nestedType (COMMA nestedType)*	SEMI			# Using
			| statementBlock												# BlockStmt
			| expr SEMI														# ExpressionStmt
			;

//cast_MOD:	'c'|'s'|'d'|'r'|;

// Tier 3
preOpExpr	:	preOP				expr;
castExpr	:	'(' qualType ')'	expr;
sizeofExpr	:	SIZEOF				expr
			|	SIZEOF			'('	expr ')';
newExpr		:	NEW		qualType? ('(' exprs ')')?;
deleteExpr	:	DELETE	(ary='['']')?	expr;

namedExprs	:	namedExpr (COMMA namedExpr)*;
namedExpr	:	(ID COLON)? expr;

exprs:	expr (COMMA expr)*;
expr:	expr	SCOPE			expr		# Tier1
	|	expr	(	postOP
				// func cast
				|	'('	namedExprs?	')'	// fcall
				|	'['	namedExprs?	']'	// op[]call
				|	memOP	ID			)	# Tier2
	|	<assoc=right>
		(	preOpExpr
		|	castExpr
		|	sizeofExpr
		|	newExpr
		|	deleteExpr					)	# Tier3
	|	expr	memptrOP		expr		# Tier4
	|	expr	powOP			expr		# Tier4_5
	|	expr	multOP			expr		# Tier5
	|	expr	addOP			expr		# Tier6
	|	expr	shiftOP			expr		# Tier7
	|	expr	orderOP			expr		# Tier8
	|	expr	equalOP			expr		# Tier9
	|	expr	bitandOP		expr		# Tier10
	|	expr	bitxorOP		expr		# Tier11
	|	expr	bitorOP			expr		# Tier12
	|	expr	andOP			expr		# Tier13
	|	expr	orOP			expr		# Tier14
	|	<assoc=right>
		expr	( assignOP
				| '?' expr ':')	expr		# Tier15
	|			'throw'			expr		# Tier16
	|	expr	','				expr		# Tier17
	|	anyId	tt_exp						# Tier100
	|	idOrLit								# Tier104
	|	'('		expr	')'					# ParenExpr
	|	AUTOINDEX							# Tier200
	;

tt_exp
	:	'<' exprs '>'
	;

idExpr: id (ASSIGN expr)?;

class_expr	: (PUB | PRIV | PROT) COLON						# AccessMod
			| STATIC	LCURLY class_extened_expr* RCURLY	# StaticDecl
			| CTOR		ctor_decl							# CtorDecl
			| ALIAS 	ID ASSIGN qualType SEMI				# Alias
			| class_extened_expr							# ClassExtendedDecl
			;

class_extened_expr	: FIELDS	LCURLY (typedIdExpr		SEMI)* RCURLY
					| FIELD				typedIdExpr		SEMI
					| PROP				typedIdExpr		SEMI
					| METH				funcDecl
					| OPERATOR			opDecl
					;

// TODO: Prop - get,set,refget
typedIdExpr	: qualType idExpr (COMMA idExpr)*;

parameter			: qualType ID?;
parameters			: LPAREN ( parameter	(COMMA parameter)* )? RPAREN;
initializationList	: COLON ID argumentList (COMMA ID argumentList)*;
argumentList		: LPAREN ( idOrLit		(COMMA idOrLit  )* )? RPAREN;

//property_expr	: ID;
ctor_decl		: parameters initializationList? statementBlock	# CtorDef;

funcDecl: (			idTplType parameters RARROW qualType
		  |			idTplType parameters
		  ) (statementBlock|'=>' expr SEMI)				# FuncMeth;

opDecl	: (			STRING_LIT parameters RARROW qualType
		  |			STRING_LIT parameters
		  ) (statementBlock|'=>' expr SEMI)				# OperatorDecl;


typePtr	: ptr=( AT_BANG | AT_QUEST | AT_PLUS | DBL_AMP | AMP | STAR | PTR_TO_ARY )		typeQualifiers
		| ary=( AT_LBRACK | LBRACK ) expr? RBRACK										typeQualifiers
		;

typeQualifiers	: typeQualifier*;
typeQualifier	: qual=(CONST | VOLATILE | MUTABLE | STABLE);

idTplType		: id tplParams?;
tplParams		: '<' qualTypeOrConst (COMMA qualTypeOrConst)* '>';

qualTypeOrConst	: qualType
				| INTEGER_LIT
				;

qualType	: typeQualifiers	( basicType | funcType | nestedType )	typePtr*;

nestedType	: idTplType (SCOPE idTplType)*;

funcType	: FUNC tplParams? parameters (RARROW qualType)?;

toplevel	: attribBlock															# Attributes
			| NS	id (SCOPE id)*	SEMI											# Namespace
			| NS	id (SCOPE id)*	LCURLY	toplevel+	RCURLY						# Namespace
			| CLASS		idTplType	LCURLY	class_expr*	RCURLY						# ClassDecl
			| STRUCT	idTplType	LCURLY	class_expr*	RCURLY						# StructDecl
			| UNION		idTplType	LCURLY	class_expr*	RCURLY						# UnionDecl
			| ENUM		id			LCURLY	idExpr (COMMA idExpr)* COMMA? RCURLY	# EnumDecl
			| FUNC	funcDecl														# FunctionDecl
			| expr SEMI																# rest
			;

prog:	toplevel+;
