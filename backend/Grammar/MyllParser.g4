parser grammar MyllParser;

options { tokenVocab = MyllLexer; }

prog		:	module? imports* /*definition?*/ levDecl*;

module		:	MODULE id SEMI;
imports		:	IMPORT id (COMMA id)* COMMA? SEMI;
// which DEFINEs can be passed directly from the compiler, all others will be discarded
//defines	:	DEFINITION id (COMMA id)* COMMA? SEMI;

// ONLY refer to levStmt, NOT the inStmt
// rename to decl?
levDecl		:	attribBlk	LCURLY	levDecl* RCURLY	# AttribDeclBlock // must be in here, since it MUST have an attrib block
			|	attribBlk	COLON 					# AttribState     // everything needs an antonym to make this work
			|	attribBlk?			inDecl			# AttribDecl;

// ONLY refer to levStmt, NOT the inStmt
levStmt		:	attribBlk?			inStmt			# AttribStmt;

attribBlk	:	LBRACK	attrib (COMMA attrib)* COMMA? RBRACK;
attrib		:	attribId
				(	'=' idOrLit
				|	'(' idOrLit (COMMA idOrLit)* COMMA? ')'
				)?;
attribId	:	id | CONST | FALL | THROW | DEFAULT;

/*
// DON'T refer to inDecl, ONLY refer to levDecl
inDecl		:	declNamespace
			|	declUsing
			|	declAlias
			|	declAspect	// TODO
			|	declConcept	// TODO
			|	declEnum
			|	declStruct
			|	declConvert
			|	declCtor
			|	declDtor
			|	declFunc
			|	declOp
			|	declVar
			;

declNamespace:	NAMESPACE	( defNamespace									);
declUsing	:	USING		( defUsing SEMI	| LCURLY attrUsing*		RCURLY	);
declAlias	:	ALIAS		( defAlias SEMI	| LCURLY attrAlias*		RCURLY	);
declAspect	:	ASPECT		( defAspect										);
declConcept	:	CONCEPT		( defConcept									);
declEnum	:	ENUM		( defEnum										);
declStruct	:	kindOfStruct( defStruct										);
declConvert	:	CONVERT		( defConvert	| LCURLY attrConvert*	RCURLY	);
declCtor	:	CTOR		( defCtor		| LCURLY attrCtor*		RCURLY	);
declDtor	:	DTOR		( defDtor										);
declFunc	:	kindOfFunc	( defFunc		| LCURLY attrFunc*		RCURLY	);
declOp		:	OPERATOR	( defOp			| LCURLY attrOp*		RCURLY	);
declVar		:	kindOfVar	( defVar		| LCURLY attrVar*		RCURLY	);

kindOfStruct:	STRUCT	|	CLASS	|	UNION;
kindOfFunc	:	FUNC	|	PROC	|	METHOD;
kindOfVar	:	VAR		|	FIELD	|	CONST	|	LET;

kindOfPassing:	COPY	|	MOVE	|	FORWARD;

attrUsing	:	defUsing	| attribBlk ( defUsing		| LCURLY attrUsing*		RCURLY | COLON );
attrAlias	:	defAlias	| attribBlk ( defAlias		| LCURLY attrAlias*		RCURLY | COLON );
attrConvert	:	defConvert	| attribBlk ( defConvert	| LCURLY attrConvert*	RCURLY | COLON );
attrCtor	:	defCtor		| attribBlk ( defCtor		| LCURLY attrCtor*		RCURLY | COLON );
attrFunc	:	defFunc		| attribBlk ( defFunc		| LCURLY attrFunc*		RCURLY | COLON );
attrOp		:	defOp		| attribBlk ( defOp			| LCURLY attrOp*		RCURLY | COLON );
attrVar		:	defVar		| attribBlk ( defVar		| LCURLY attrVar*		RCURLY | COLON );

//// TODO the 3 kinds of attr each?
//attrUsing	:	attribBlk?	defUsing;
//attrAlias	:	attribBlk?	defAlias;
//attrConvert	:	attribBlk?	defConvert;
//attrCtor	:	attribBlk?	defCtor;
//attrFunc	:	attribBlk?	defFunc;
//attrOp		:	attribBlk?	defOp;
//attrVar		:	attribBlk?	defVar;

defNamespace:	id (SCOPE id)*
				(	SEMI
				|	COLON
				|	LCURLY	levDecl*		RCURLY);

defUsing	:	typespecsNested;
defAlias	:	id	tplParams?	ASSIGN	typespec;	// TODO: needs COMMA multiple support

defAspect	:	id	tplParams?;		// TODO
defConcept	:	id	tplParams?		// TODO
				(COLON	typespecsNested)?
						LCURLY	levDecl*		RCURLY;
defEnum		:	id
				(COLON	bases=typespecBasic)?	// TODO: enum inheritance
					LCURLY	idExprs			RCURLY;
defStruct	:	id	tplParams?
				(COLON		bases=typespecsNested)?
				(REQUIRES 	reqs=typespecsNested)?	// TODO
					LCURLY	levDecl*		RCURLY;

defConvert	:	(	RARROW		to=typespec					// convert -> TYPE	- convert to TYPE	- operator TYPE
				|				from=typespec	id?	RARROW	// convert TYPE ->	- convert from TYPE	- ctor( TYPE ) // not very analogous to the return type definition
				|	LARROW		from=typespec	id?			// convert <- TYPE	- convert from TYPE	- ctor( TYPE )
				)
				funcBody;
defCtor		:	(	kindOfPassing				id?
				|	CONVERT	LARROW?	typespec	id?
				|	CONVERT			typespec	id?	RARROW?
				|	funcTypeDef?	initList?
				)
				funcBody;
defDtor		:	(LPAREN RPAREN)?
				funcBody;
defFunc		:	id			tplParams?	funcTypeDef?
				(RARROW		typespec)?
				(REQUIRES	typespecsNested)?	// TODO
				funcBody;
defOp		:	(	kindOfPassing	ASSIGN	id?
				|	CONVERT 		RARROW?
				|	STRING_LIT		tplParams?	funcTypeDef?
					(RARROW			typespec)?
					(REQUIRES		typespecsNested)?	// TODO
				)
				funcBody;

defVar		:	typedIdAcors;




/*
opDef		:	STRING_LIT	tplParams?	funcTypeDef?
				(RARROW		typespec)?
				(REQUIRES	typespecsNested)?	// TODO
				funcBody;
*/

// The great reordering Part 2, everything below still TODO


comment		:	COMMENT;

// handled by ToPreOp because of collisions
preOP		:	v=(	'++' | '--' | '+' | '-' | '!' | '~' | '*' | '&' );

// all operators below are handled by ToOp
postOP		:	v=(	'++' | '--' );

powOP		:		'*''*';
multOP		:	v=(	'*' | '/' | '%' | '&' | '·' | '×' | '÷' );
addOP		:	v=(	'+' | '-' | '^' | '|' ); // split xor and or?
shiftOP		:		'<<' | '>''>';

cmpOp       :   	'<=>';
relOP		:	v=(	'<=' | '>=' | '<' | '>' );
equalOP		:	v=(	'==' | '!=' );

andOP		:		'&&';
orOP		:		'||';

nulCoalOP	:		'?:';

memAccOP	:	v=(	'.'  | '?.'  | '->'  );
memAccPtrOP	:	v=(	'.*' | '?.*' | '->*' );
//memAccOP	:	v=(	'.'  | '..'  | '?.'  | '?..'  | '->'  );
//memAccPtrOP	:	v=(	'.*' | '..*' | '?.*' | '?..*' | '->*' );

assignOP	:		'=';
aggrAssignOP:	v=(	'**=' | '*=' | '/=' | '%=' | '&=' |	'·=' |	'×=' |	'÷=' |
					'+='  | '-=' | '|=' | '^=' | '<<=' | '>>=' );

lit			:	CLASS_LIT | HEX_LIT | OCT_LIT | BIN_LIT | INTEGER_LIT | FLOAT_LIT | STRING_LIT | CHAR_LIT | BOOL_LIT | NUL;
wildId		:	AUTOINDEX | USCORE;
id			:	ID;
idOrLit		:	id | lit;

// +++ handled
specialType	:	v=( AUTO | VOID | BOOL );
charType	:	v=( CHAR | CODEPOINT | STRING );
floatingType:	v=( FLOAT | F128 | F64 | F32 | F16 ); // 80 and 96 bit?
binaryType	:	v=( BYTE | B64 | B32 | B16 | B8 );
signedIntType:	v=( INT  | ISIZE | I64 | I32 | I16 | I8 );
unsignIntType: 	v=( UINT | USIZE | U64 | U32 | U16 | U8 );

qual		:	v=( CONST | MUTABLE | VOLATILE | STABLE );

typePtr		:	qual*
				(	ptr=( DBL_AMP | AMP | STAR | PTR_TO_ARY )
				|	ary=( AT_LBRACK | LBRACK ) expr? RBRACK )
				suffix=( EM | PLUS | QM )?;

idTplArgs	:	id	tplArgs?;

typespec	:	qual*
				(	typespecBasic	typePtr*
				|	FUNC			typePtr*	typespecFunc
				|	typespecNested	typePtr*);

typespecBasic	:	specialType
				|	charType
				|	floatingType
				|	binaryType
				|	signedIntType
				|	unsignIntType;

typespecFunc	:	funcTypeDef (RARROW typespec)?;

// TODO different order than ScopedExpr
typespecNested	:			idTplArgs
					(SCOPE	idTplArgs)*
					(SCOPE	v=CTOR)?;
typespecsNested	:	typespecNested (COMMA typespecNested)* COMMA?;

// --- handled

arg			:	(id COLON)? expr;
args		:	arg (COMMA arg)* COMMA?;
funcCall	:	ary=( QM_LPAREN | LPAREN )	args?	RPAREN;
indexCall	:	ary=( QM_LBRACK | LBRACK )	args	RBRACK;

param		:	typespec id?;
funcTypeDef	:	LPAREN	(param (COMMA param)* COMMA?)?	RPAREN;

// can't contain expr, will fck up through idTplArgs with multiple templates (e.g. op | from enums)
tplArg		:	lit | typespec;
tplArgs		:	LT tplArg	(COMMA tplArg)*	COMMA? GT;
tplParams	:	LT id		(COMMA id)*		COMMA? GT;

threeWay	:	(relOP | equalOP)	COLON	expr;

capture		:	LBRACK	args?	RBRACK;

// Tier 3
//cast: nothing = static, ? = dynamic, ! = const & reinterpret
// TODO REMOVE THEM ALL, IN CODE TOO
//					xxx
//preOpExpr	:	preOP;
//castExpr	:	(MOVE|LPAREN (QM|EM)? typespec RPAREN);
//sizeofExpr	:	SIZEOF;
//deleteExpr	:	DELETE	(ary='['']')?;

// The order here is significant, it determines the operator precedence
expr		:	(idTplArgs	SCOPE)+	idTplArgs	# ScopedExpr
			|	expr
					(	postOP
					|	funcCall
					|	indexCall
					|	memAccOP	idTplArgs )	# PostExpr
			| // can not even be associative without a contained expr
				NEW	typespec?	funcCall?		# NewExpr
			| // inherently <assoc=right>, so no need to tell ANTLR
				(	LPAREN
					(	COPY // cast that forces a copy?
					|	MOVE
					|	FORWARD
					|	(QM|EM)? typespec	) RPAREN
				|	SIZEOF
				|	DELETE	( ary='['']' )?
				|	preOP
				)						expr	# PreExpr // this visitor is unused
			|	expr	memAccPtrOP		expr	# MemPtrExpr
			| <assoc=right>
				expr	powOP			expr	# PowExpr
			|	expr	multOP			expr	# MultExpr
			|	expr	addOP			expr	# AddExpr
			|	expr	shiftOP			expr	# ShiftExpr
			|	expr	cmpOp			expr	# ComparisonExpr
			|	expr	relOP			expr	# RelationExpr
			|	expr	equalOP			expr	# EqualityExpr
			|	expr	andOP			expr	# AndExpr
			|	expr	orOP			expr	# OrExpr
			|	expr	TRP_POINT		expr	# RangeExpr
			|	expr	nulCoalOP		expr	# NullCoalesceExpr
			// TODO: 2-way-cond and throw were in the same level, test if this still works fine
			| <assoc=right>
				expr	QM expr COLON	expr	# ConditionalExpr
			| <assoc=right>
				expr	DBL_QM threeWay+ (COLON expr)?	# ThreeWayConditionalExpr
			| <assoc=right>
				THROW	expr					# ThrowExpr		// Good or not?
			|	FUNC capture? tplParams? funcTypeDef
				(RARROW typespec)?	funcBody	# LambdaExpr	// TODO CST
			|	LPAREN	expr	RPAREN			# ParenExpr
			|	wildId							# WildIdExpr
			|	lit								# LiteralExpr	// TODO
			|	idTplArgs						# IdTplExpr
			;

idAccessor	:	id	(LCURLY accessorDef+ RCURLY)?	(ASSIGN expr)?;
idExpr		:	id									(ASSIGN expr)?;
idAccessors	:	idAccessor	(COMMA idAccessor)*	COMMA?;
idExprs		:	idExpr		(COMMA idExpr)*		COMMA?;
typedIdAcors:	typespec 	idAccessors	SEMI;

caseBlock	:	CASE expr (COMMA expr)*
				(	COLON		levStmt* (FALL SEMI)?
				|	LCURLY		levStmt* (FALL SEMI)? RCURLY
				|	PHATRARROW	levStmt);
defaultBlock:	(ELSE|DEFAULT)
				(	COLON		levStmt*
				|	LCURLY		levStmt* RCURLY
				|	PHATRARROW	levStmt);

initList	:	COLON id funcCall (COMMA id funcCall)* COMMA?;
// TODO: initList needs to support ": ctor()"

// is just SEMI as well in levStmt->inStmt
funcBody	:	PHATRARROW expr SEMI
			|	levStmt;
accessorDef	:	attribBlk?	qual* v=( GET | REFGET | SET ) funcBody;
funcDef		:	id			tplParams?	funcTypeDef?
				(RARROW		typespec)?
				(REQUIRES	typespecsNested)?	// TODO
				funcBody;
opDef		:	STRING_LIT	tplParams?	funcTypeDef?
				(RARROW		typespec)?
				(REQUIRES	typespecsNested)?	// TODO
				funcBody;

// remove this?
condThen	:	LPAREN	expr	RPAREN	levStmt;
		//	|			expr			levStmt; // bench if this would hurt
		//	|			expr	COLON	levStmt; // try if this may work

// DON'T refer to inDecl, ONLY refer to levDecl
// ns, class, enum, func, ppp, c/dtor, alias, static
inDecl		:	NAMESPACE id (SCOPE id)*
				(		SEMI
				|		COLON
				|		LCURLY	levDecl*		RCURLY)	# Namespace
			|	v=( STRUCT | CLASS | UNION ) id tplParams?
				(COLON		bases=typespecsNested)?
				(REQUIRES 	reqs=typespecsNested)?	// TODO
						LCURLY	levDecl*		RCURLY	# StructDecl
			|	CONCEPT	id tplParams?		// TODO
				(COLON	typespecsNested)?
						LCURLY	levDecl*		RCURLY	# ConceptDecl
			|	ASPECT	id								# AspectDecl // TODO aspect
			|	ENUM	id
				(COLON	bases=typespecBasic)?	// TODO: enum inheritance
						LCURLY	idExprs			RCURLY	# EnumDecl
			|	v=( FUNC | PROC | METHOD )
				(				funcDef
				|		LCURLY	funcDef*		RCURLY)	# FunctionDecl
			|	(	(COPY|MOVE)	OPERATOR STRING_LIT?	id?
				|	CONVERT (OPERATOR|FUNC|PROC|METHOD)	RARROW	typespec
				|	OPERATOR (COPY|MOVE) STRING_LIT?	id?
				|	(OPERATOR|FUNC|PROC|METHOD) CONVERT	RARROW	typespec
				)							funcBody	# OpDecl
			|	OPERATOR								// TODO remove one of the OpDecl
				(				opDef
				|		LCURLY	opDef*			RCURLY)	# OpDecl
			|	USING		typespecsNested		SEMI	# UsingDecl
			// TODO: alias needs template support
			|	ALIAS id ASSIGN	typespec 		SEMI	# AliasDecl
			|	v=( VAR | FIELD | CONST | LET )
				(				typedIdAcors
				|		LCURLY	typedIdAcors*	RCURLY)	# VariableDecl
			|	(	DEFAULT				CTOR
				|	(COPY|MOVE|FORWARD)	CTOR	id?
				|	CONVERT				CTOR	/*LARROW?*/ typespec id?
				|	CTOR	DEFAULT
				|	CTOR	(COPY|MOVE|FORWARD)	id?
				|	CTOR	CONVERT	/*LARROW?*/	typespec id?
				|	CTOR	funcTypeDef?		initList?
				)							funcBody	# CtorDecl
			|	DTOR	(LPAREN RPAREN)?	funcBody	# DtorDecl
			;

// DON'T refer to inStmt, ONLY refer to levStmt
inStmt		:	SEMI								# EmptyStmt
			|	LCURLY	levStmt*		RCURLY		# BlockStmt
			|	USING	typespecsNested	SEMI		# UsingStmt
			// TODO: alias needs template support
			|	ALIAS id ASSIGN	typespec 	SEMI	# AliasStmt
			|	v=( VAR | FIELD | CONST | LET )
				(	LCURLY	typedIdAcors*	RCURLY
				|			typedIdAcors		)	# VariableStmt
			|	RETURN	expr?		SEMI			# ReturnStmt
			|	DO RETURN expr?
				IF LPAREN expr RPAREN	SEMI		# ReturnIfStmt
			|	THROW	expr			SEMI		# ThrowStmt
			|	BREAK	INTEGER_LIT?	SEMI		# BreakStmt
			|	IF			condThen
				(ELSE IF	condThen)*	// helps with formatting properly and de-nesting the AST
				(ELSE		levStmt)?				# IfStmt
			|	SWITCH	LPAREN cond=expr RPAREN	LCURLY
				caseBlock+ 	defaultBlock?	RCURLY	# SwitchStmt
			|	LOOP	body=levStmt				# LoopStmt
			|	FOR LPAREN init=levStmt	// TODO: add the syntax: for( a : b )
				cond=expr? SEMI
				iter=expr? RPAREN	body=levStmt
				(ELSE				els=levStmt)?	# ForStmt
			|	WHILE	LPAREN cond=expr RPAREN
						body=levStmt
				(ELSE	els=levStmt)?				# WhileStmt
			|	DO		body=levStmt
				WHILE	LPAREN cond=expr RPAREN		# DoWhileStmt
			|	DO? count=expr TIMES
				(name=id ((PLUS|MINUS) INTEGER_LIT)? )?
						body=levStmt				# TimesStmt
			|	TRY		levStmt
			// funcTypeDef is wrong, but works for easy cases
				(CATCH	funcTypeDef		levStmt)+	# TryCatchStmt		// TODO CST
			|	DEFER	levStmt						# DeferStmt			// TODO CST, scope_exit(,fail,success)
													// #include <experimental/scope>
													// std::experimental::scope_exit guard([]{ cout << "Exit!" << endl; });
			| 	expr	(assignOP		expr)+ SEMI	# MultiAssignStmt
			| 	expr	aggrAssignOP	expr SEMI	# AggrAssignStmt
			|	expr	SEMI						# ExpressionStmt
			;
