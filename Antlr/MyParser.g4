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
bitAndOP	:					'&';
bitXorOP	:							'^';
bitOrOP		:									'|';
andOP		:					'&&';
orOP		:									'||';

memOP		:	'.'  | '->';
memPtrOP	:	'.*' | '->*';

orderOP		:	'<'	|'<='|'>'|'>=';
equalOP		:	'=='|'!=';

comment		:	COMMENT;

id			:	ID;
anyId		:	ID | AUTOINDEX | USCORE;
idOrLit		:	ID | INTEGER_LIT | HEX_LIT | FLOAT_LIT | STRING_LIT | CHAR_LIT | BOOL_LIT | NUL;

charType	:	CHAR | CODEPOINT | STRING;

floatingType:	FLOAT | F80 | F64 | F32 | F16;

binaryType	:	BYTE | B64 | B32 | B16 | B8;

integerType	:	INT | UINT | ISIZE | USIZE
			|	I64 | I32 | I16 | I8
			|	U64 | U32 | U16 | U8
			;

basicType	:	AUTO
			|	VOID
			|	BOOL
			|	charType
			|	floatingType
			|	binaryType
			|	integerType
			;

typeQual	:	qual=( CONST | MUTABLE | VOLATILE | STABLE );

typePtr		:	typeQual*	( ptr=( AT_BANG | AT_QUEST | AT_PLUS | DBL_AMP | AMP | STAR | PTR_TO_ARY )
							| ary=( AT_LBRACK | LBRACK ) expr? RBRACK )
			;

idTplArgs	:	id tplArgs?;
nestedType	:	idTplArgs (SCOPE idTplArgs)*;

funcType	:	FUNC tplArgs? funcDef (RARROW typeSpec)?;

typeSpec	:	typeQual*	( basicType | funcType | nestedType )	typePtr*;

arg			:	(id COLON)? expr;
funcCall	:	LPAREN	(arg (COMMA arg)*)?	RPAREN;
indexCall	:	LBRACK	(arg (COMMA arg)*)	RBRACK;

param		:	typeSpec id?;
funcDef		:	LPAREN (param (COMMA param)*)? RPAREN;

tplArg		:	typeSpec | INTEGER_LIT;
tplArgs		:	LT tplArg (COMMA tplArg)* GT;

tplParams	:	LT id (COMMA id)* GT;

exprs		:	expr (COMMA expr)*;
tt_exp		:	LT exprs GT;

// Tier 3
//cast_MOD:	'c'|'s'|'d'|'r'|;
preOpExpr	:	preOP					expr;
castExpr	:	LPAREN typeSpec RPAREN	expr;
sizeofExpr	:	SIZEOF					expr;
newExpr		:	NEW		typeSpec?	funcCall?;
deleteExpr	:	DELETE	(ary='['']')?	expr;

expr		:	expr	SCOPE			expr	# Tier1
			|	expr
				(	postOP
				// func cast
				|	funcCall
				|	indexCall
				|	memOP	id	)				# Tier2
			|	<assoc=right>
				(	preOpExpr
				|	castExpr
				|	sizeofExpr
				|	newExpr
				|	deleteExpr	)				# Tier3
			|	expr	memPtrOP		expr	# Tier4
			|	expr	powOP			expr	# Tier4_5
			|	expr	multOP			expr	# Tier5
			|	expr	addOP			expr	# Tier6
			|	expr	shiftOP			expr	# Tier7
			|	expr	orderOP			expr	# Tier8
			|	expr	equalOP			expr	# Tier9
			|	expr	bitAndOP		expr	# Tier10
			|	expr	bitXorOP		expr	# Tier11
			|	expr	bitOrOP			expr	# Tier12
			|	expr	andOP			expr	# Tier13
			|	expr	orOP			expr	# Tier14
			|	<assoc=right>
				expr	( assignOP
						| '?' expr ':')	expr	# Tier15
			|			'throw'			expr	# Tier16
			//|	expr	','				expr	# Tier17
			|	anyId	tt_exp					# Tier100
			|	idOrLit							# Tier104
			|	'('		expr	')'				# ParenExpr
			|	AUTOINDEX						# Tier200
			;

idExpr		:	id (ASSIGN expr)?;
// TODO: Prop - get,set,refget
typedIdExprs:	typeSpec idExpr (COMMA idExpr)*;


attrib		:	id	(	'=' idOrLit
					|	'(' idOrLit (COMMA idOrLit)* ')'
					)?;
attribBlk	:	LBRACK attrib (COMMA attrib)* RBRACK;

stmt		:	USING	nestedType (COMMA nestedType)*	SEMI				# Using
			|	VAR		typedIdExprs SEMI									# VariableDecl
			|	CONST	typedIdExprs SEMI									# VariableDecl
			|	RETURN	expr	SEMI										# ReturnStmt
			|	BREAK			SEMI										# BreakStmt
			|	IF	LPAREN expr RPAREN stmt ( ELSE stmt )?					# IfStmt
			|	FOR	LPAREN stmt expr SEMI expr RPAREN stmt ( ELSE stmt )?	# ForStmt
			|	expr TIMES id?		stmt									# TimesStmt
			|	expr '..' expr		stmt									# EachStmt
			| 	(expr	assignOP)+	expr	SEMI							# AssignmentStmt
			|	stmtBlk														# BlockStmt
			|	expr SEMI													# ExpressionStmt
			;
stmtBlk		:	LCURLY	stmt*	RCURLY;


classDef	:	(PUB | PRIV | PROT) COLON						# AccessMod
			|	CTOR	ctorDecl								# ClassCtorDecl
			|	ALIAS 	id ASSIGN typeSpec SEMI					# Alias
			|	STATIC	LCURLY classExtDef* RCURLY				# StaticDecl
			|	classExtDef										# ClassExtendedDecl
			;

classExtDef	:	FIELDS	LCURLY (typedIdExprs		SEMI)* RCURLY
			|	FIELD			typedIdExprs		SEMI
			|	PROP			typedIdExprs		SEMI
			|	METH			funcDecl
			|	OPERATOR		opDecl
			;

initList	:	COLON id funcCall (COMMA id funcCall)*;
ctorDecl	:	funcDef initList?	stmtBlk						# CtorDef;

funcDecl	:	(		id tplParams? funcDef RARROW typeSpec
				|		id tplParams? funcDef
				) (stmtBlk|'=>' expr SEMI)				# FuncMeth;

opDecl		:	(		STRING_LIT funcDef RARROW typeSpec
				|		STRING_LIT funcDef
				) (stmtBlk|'=>' expr SEMI)				# OperatorDecl;

topLevel	:	attribBlk																# Attributes
			|	NS	id (SCOPE id)*		SEMI											# Namespace
			|	NS	id (SCOPE id)*		LCURLY	topLevel+	RCURLY						# Namespace
			|	CLASS	id tplParams?	LCURLY	classDef*	RCURLY						# ClassDecl
			|	STRUCT	id tplParams?	LCURLY	classDef*	RCURLY						# StructDecl
			|	UNION	id tplParams?	LCURLY	classDef*	RCURLY						# UnionDecl
			|	ENUM	id				LCURLY	idExpr (COMMA idExpr)* COMMA? RCURLY	# EnumDecl
			|	FUNC	funcDecl														# FunctionDecl
			|	expr SEMI																# rest
			;

prog		:	topLevel+;
