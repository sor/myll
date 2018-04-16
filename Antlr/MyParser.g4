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

typePtr		:	typeQual*   ( ptr=( AT_BANG | AT_QUEST | AT_PLUS | DBL_AMP | AMP | STAR | PTR_TO_ARY )
			                | ary=( AT_LBRACK | LBRACK ) expr? RBRACK )
			;

nestedType	:	idTplType (SCOPE idTplType)*;

funcType	:	FUNC tplParams? funcDef (RARROW typeSpec)?;

typeSpec	:	typeQual*	( basicType | funcType | nestedType )	typePtr*;

typeSpecOrLit:	typeSpec
			|	INTEGER_LIT;

// reffed before
tplParams	:	'<' typeSpecOrLit (COMMA typeSpecOrLit)* '>';
idTplType	:	id tplParams?;

// Tier 3
//cast_MOD:	'c'|'s'|'d'|'r'|;
preOpExpr	:	preOP				expr;
castExpr	:	'(' typeSpec ')'	expr;
sizeofExpr	:	SIZEOF				expr;
newExpr		:	NEW		typeSpec? ('(' exprs ')')?;
deleteExpr	:	DELETE	(ary='['']')?	expr;

arg	        :	(ID COLON)? expr;
args	    :	arg (COMMA arg)*;
funcCall	:	LPAREN  args?   RPAREN;
indexCall   :   LBRACK  args    RBRACK;

param		:	typeSpec ID?;
params		:	param (COMMA param)*;
funcDef     :   LPAREN params? RPAREN;

expr		:	expr	SCOPE			expr	# Tier1
			|	expr
				(	postOP
				// func cast
				|	funcCall
				|	indexCall
				|	memOP	ID	)				# Tier2
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
exprs		:	expr (COMMA expr)*;
tt_exp		:	'<' exprs '>';

idExpr		:	id (ASSIGN expr)?;
// TODO: Prop - get,set,refget
typedIdExprs:	typeSpec idExpr (COMMA idExpr)*;


attrib		:	ID	(	'=' idOrLit
					|	'(' idOrLit (COMMA idOrLit)* ')'
					)?;
attribBlk	:	LBRACK attrib (COMMA attrib)* RBRACK;


stmt		:	USING	nestedType (COMMA nestedType)*	SEMI				# Using
			|	VAR		typedIdExprs SEMI									# VariableDecl
			|	CONST	typedIdExprs SEMI									# VariableDecl
			|	IF		LPAREN expr RPAREN stmt ( ELSE stmt )?				# IfStmt
			|	FOR		LPAREN stmt expr SEMI expr RPAREN stmt ( ELSE stmt )?	# ForStmt
			|	expr TIMES ID?		stmt									# TimesStmt
			|	expr '..' expr		stmt									# EachStmt
			//| (expr	assign_OP)+ expr	SEMI							# AssignmentStmt
			|	RETURN	expr	SEMI										# ReturnStmt
			|	BREAK			SEMI										# BreakStmt
			|	stmtBlk														# BlockStmt
			|	expr SEMI													# ExpressionStmt
			;
stmtBlk		:	LCURLY	stmt*	RCURLY;


classDef	:	(PUB | PRIV | PROT) COLON						# AccessMod
			|	CTOR	ctorDecl								# ClassCtorDecl
			|	ALIAS 	ID ASSIGN typeSpec SEMI					# Alias
			|	STATIC	LCURLY classExtDef* RCURLY				# StaticDecl
			|	classExtDef										# ClassExtendedDecl
			;

classExtDef	:	FIELDS	LCURLY (typedIdExprs		SEMI)* RCURLY
			|	FIELD			typedIdExprs		SEMI
			|	PROP			typedIdExprs		SEMI
			|	METH			funcDecl
			|	OPERATOR		opDecl
			;

initList	:	COLON ID funcCall (COMMA ID funcCall)*;
ctorDecl	:	funcDef initList?	stmtBlk						# CtorDef;

funcDecl	:	(		idTplType funcDef RARROW typeSpec
				|		idTplType funcDef
				) (stmtBlk|'=>' expr SEMI)				# FuncMeth;

opDecl		:	(		STRING_LIT funcDef RARROW typeSpec
				|		STRING_LIT funcDef
				) (stmtBlk|'=>' expr SEMI)				# OperatorDecl;

topLevel	:	attribBlk															# Attributes
			|	NS	id (SCOPE id)*	SEMI											# Namespace
			|	NS	id (SCOPE id)*	LCURLY	topLevel+	RCURLY						# Namespace
			|	CLASS	idTplType	LCURLY	classDef*	RCURLY						# ClassDecl
			|	STRUCT	idTplType	LCURLY	classDef*	RCURLY						# StructDecl
			|	UNION	idTplType	LCURLY	classDef*	RCURLY						# UnionDecl
			|	ENUM	id			LCURLY	idExpr (COMMA idExpr)* COMMA? RCURLY	# EnumDecl
			|	FUNC	funcDecl													# FunctionDecl
			|	expr SEMI															# rest
			;

prog		:	topLevel+;

