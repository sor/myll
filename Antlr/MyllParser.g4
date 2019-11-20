parser grammar MyllParser;

options { tokenVocab = MyllLexer; }

comment		:	COMMENT;

// all operators handled by ToOp except mentioned
postOP		:	v=(	'++' | '--' );

// handled by ToPreOp because of collisions
preOP		:	v=(	'++' | '--' | '+' | '-' | '!' | '~' | '*' | '&' );

powOP		:		'*''*';
multOP		:	v=(	'*' | '/' | '%' | '&' | '·' | '×' | '÷' );
addOP		:	v=(	'+' | '-' | '|' | '^' );
shiftOP		:		'<<' | '>''>';

cmpOp       :   	'<=>';
orderOP		:	v=(	'<=' | '>=' | '<' | '>' );
equalOP		:	v=(	'==' | '!=' );

andOP		:		'&&';
orOP		:		'||';

nulCoalOP	:		'??' | '?:';

memAccOP	:	v=(	'.'  | '?.'  | '->'  );
memAccPtrOP	:	v=(	'.*' | '?.*' | '->*' );

// handled by ToAssignOp because of collisions
assignOP	:		'=';
moveOP		:		'=<';
aggrAssignOP:	v=(	'**=' | '*=' | '/=' | '%=' | '&=' |	'·=' |	'×=' |	'÷='
				|	'+='  | '-=' | '|=' | '^=' | '<<=' | '>>=' );

lit			:	CLASS_LIT | HEX_LIT | OCT_LIT | BIN_LIT | INTEGER_LIT | FLOAT_LIT | STRING_LIT | CHAR_LIT | BOOL_LIT | NUL;
wildId		:	AUTOINDEX | USCORE;
id			:	ID;
idOrLit		:	id | lit;

// +++ handled
specialType	:	v=( AUTO | VOID | BOOL );
charType	:	v=( CHAR | CODEPOINT | STRING );
floatingType:	v=( FLOAT | DOUBLE | F80 | F64 | F32 | F16 );
binaryType	:	v=( BYTE | B64 | B32 | B16 | B8 );
signedIntType:	v=( INT  | ISIZE | I64 | I32 | I16 | I8 );
unsignIntType:  v=( UINT | USIZE | U64 | U32 | U16 | U8 );

qual		:	v=( CONST | MUTABLE | VOLATILE | STABLE );

typePtr		:	qual*	( ptr=( AT_BANG | AT_QUEST | AT_PLUS | DBL_AMP | AMP | STAR | PTR_TO_ARY )
						| ary=( AT_LBRACK | LBRACK ) expr? RBRACK )
			;

idTplArgs	:	id tplArgs?;

typespec		:	qual*	( typespecBasic
							| typespecFunc
							| typespecNested )	typePtr*;

typespecBasic	:	specialType
				|	charType
				|	floatingType
				|	binaryType
				|	signedIntType
				|	unsignIntType;

typespecFunc	:	FUNC tplArgs? funcTypeDef (RARROW typespec)?;

typespecNested	:	idTplArgs (SCOPE idTplArgs)*;
typespecsNested	:	typespecNested (COMMA typespecNested)* COMMA?;

// --- handled

arg			:	(id COLON)? expr;
args		:	(arg (COMMA arg)* COMMA?);
funcCall	:	ary=( QM_LPAREN | LPAREN )	args?	RPAREN;
indexCall	:	ary=( QM_LBRACK | LBRACK )	args	RBRACK;

param		:	typespec id?;
funcTypeDef	:	LPAREN (param (COMMA param)*)? RPAREN;

// this expr needs to be a constexpr and can be an id (from a surrounding template)
// TODO evaluate if 'id' is really necessary/beneficial here or just let expr handle it
tplArg		:	typespec | id | expr; //INTEGER_LIT | id;
tplArgs		:	LT tplArg (COMMA tplArg)* GT;

tplParams	:	LT id (COMMA id)* GT;

// Tier 3
//cast: nothing = static, ? = dynamic, ! = const & reinterpret
preOpExpr	:	preOP					expr;
castExpr	:	LPAREN (QM|EM)? typespec RPAREN	expr;
sizeofExpr	:	SIZEOF					expr;
newExpr		:	NEW		typespec?	funcCall?;
deleteExpr	:	DELETE	(ary='['']')?	expr;

expr		:	(idTplArgs	SCOPE)+		expr	# ScopedExpr
			|	expr
				(	postOP
				|	funcCall
				|	indexCall
				|	memAccOP	idTplArgs)		# PostExpr
			| <assoc=right>
				(	preOpExpr
				|	castExpr
				|	sizeofExpr
				|	newExpr
				|	deleteExpr	)				# PreExpr
			|	expr	memAccPtrOP		expr	# MemPtrExpr
			| <assoc=right>
				expr	powOP			expr	# PowExpr
			|	expr	multOP			expr	# MultExpr
			|	expr	addOP			expr	# AddExpr
			|	expr	shiftOP			expr	# ShiftExpr
			|	expr	cmpOp			expr	# ComparisonExpr
			|	expr	orderOP			expr	# RelationExpr
			|	expr	equalOP			expr	# EqualityExpr
			|	expr	andOP			expr	# AndExpr
			|	expr	orOP			expr	# OrExpr
			|	expr	nulCoalOP		expr	# NullCoalesceExpr
			| <assoc=right>
				expr	QM expr COLON	expr	# ConditionalExpr
			|	LPAREN	expr	RPAREN			# ParenExpr
			|	wildId							# WildIdExpr
			|	lit								# LiteralExpr
			|	idTplArgs						# IdTplExpr
			;

idAccessor	:	id	(LCURLY accessorDef+ RCURLY)?	(ASSIGN expr)?;
idExpr		:	id									(ASSIGN expr)?;
idAccessors	:	idAccessor	(COMMA idAccessor)*	COMMA?;
idExprs		:	idExpr		(COMMA idExpr)*		COMMA?;
typedIdAcors:	typespec 	idAccessors	SEMI;

attrib		:	id	(	'=' idOrLit
					|	'(' idOrLit (COMMA idOrLit)* ')'
					)?;
attribBlk	:	LBRACK	attrib (COMMA attrib)* COMMA? RBRACK;

caseStmt	:	CASE expr (COMMA expr)* COLON levStmt+ (FALL SEMI)?;

initList	:	COLON id funcCall (COMMA id funcCall)* COMMA?;

// is just SEMI as well in levStmt/inStmt
funcBody	:	PHATRARROW LCURLY expr RCURLY
 			|	PHATRARROW expr SEMI
			|	levStmt;
accessorDef	:	a=( PUB | PROT | PRIV )? qual* v=( GET | REFGET | SET ) funcBody;
funcDef		:	id			tplParams?	funcTypeDef (RARROW typespec)?
				(REQUIRES typespecsNested)?		// TODO
				funcBody;
opDef		:	STRING_LIT	tplParams?	funcTypeDef (RARROW typespec)?
				(REQUIRES typespecsNested)?		// TODO
				funcBody;

prog		:	levDecl+;

// only refer to these lev* levels, not the in*
levDecl		:	attribBlk	LCURLY	levDecl	RCURLY	// must be in here, since it MUST have an attrib block
			//|	attribBlk	COLON
			|	attribBlk?	( inAnyStmt | inDecl );
levStmt		:	attribBlk?	( inAnyStmt | inStmt );
levStmtDef	:	attribBlk?	( inAnyStmt );

// ns, class, enum, func, ppp, c/dtor, alias, static
inDecl		:	NS id (SCOPE id)* SEMI						# Namespace // or better COLON
			|	NS id (SCOPE id)* LCURLY levDecl+ RCURLY	# Namespace
			|	v=( STRUCT | CLASS | UNION ) id tplParams?
				(COLON		bases=typespecsNested)?
				(REQUIRES 	reqs=typespecsNested)?	// TODO
						LCURLY	levDecl*	RCURLY	# StructDecl
			|	CONCEPT	id tplParams?		// TODO
				(COLON	typespecsNested)?
						LCURLY	levDecl*	RCURLY	# ConceptDecl
			// TODO aspect
			|	ENUM id	LCURLY	idExprs		RCURLY	# EnumDecl
			|	FUNC	LCURLY	funcDef*	RCURLY	# FunctionDecl
			|	FUNC			funcDef				# FunctionDecl
			|	OPERATOR LCURLY opDef*		RCURLY	# OpDecl
			|	OPERATOR		opDef				# OpDecl
// class only:
			|	a=( PUB | PROT | PRIV ) COLON		# AccessMod
			|	CTOR funcTypeDef initList?	(SEMI | levStmt) # CtorDecl
			|	COPYCTOR funcTypeDef initList?	(SEMI | levStmt) # CtorDecl
			|	MOVECTOR funcTypeDef initList?	(SEMI | levStmt) # CtorDecl
			|	DTOR LPAREN RPAREN			(SEMI | levStmt) # DtorDecl
			//|	STATIC	LCURLY	levDecl* RCURLY		# StaticDecl
			//|	STATIC			levDecl				# StaticDecl
			;

// using, var, const: these are both Stmt and Decl
inAnyStmt	:	USING			typespecsNested	SEMI	# Using
			|	ALIAS id ASSIGN	typespec 		SEMI	# AliasDecl
			|	VAR		LCURLY	typedIdAcors*	RCURLY	# VariableDecl
			|	VAR				typedIdAcors			# VariableDecl
			|	CONST	LCURLY	typedIdAcors*	RCURLY	# VariableDecl
			|	CONST			typedIdAcors			# VariableDecl
			;

inStmt		:	SEMI								# EmptyStmt
			|	LCURLY	levStmt*	RCURLY			# BlockStmt
			|	RETURN	expr?		SEMI			# ReturnStmt
			|	THROW	expr		SEMI			# ThrowStmt
			|	BREAK	INTEGER_LIT	SEMI			# BreakStmt
			|	IF		LPAREN expr RPAREN
				levStmt	(ELSE levStmt)?				# IfStmt
			|	SWITCH	LPAREN expr RPAREN	LCURLY
				caseStmt+ 	(ELSE levStmt+)? RCURLY	# SwitchStmt
			|	LOOP	levStmt						# LoopStmt
			|	FOR		LPAREN levStmtDef expr SEMI expr RPAREN
				levStmt	(ELSE levStmt)?				# ForStmt
			|	WHILE	LPAREN expr RPAREN
				levStmt	(ELSE levStmt)?				# WhileStmt
			|	DO levStmt
				WHILE	LPAREN expr RPAREN SEMI?	# DoWhileStmt
			|	expr TIMES			id?	levStmt		# TimesStmt
			|	expr DBL_POINT expr id?	levStmt		# EachStmt
			| 	(expr	assignOP)+		expr SEMI	# MultiAssignStmt
			| 	expr	aggrAssignOP	expr SEMI	# AggrAssignStmt
			|	expr SEMI							# ExpressionStmt
			;
