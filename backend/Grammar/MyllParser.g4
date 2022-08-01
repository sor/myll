parser grammar MyllParser;

options { tokenVocab = MyllLexer; }

comment		:	COMMENT;

prog		:	module?
				imports*
				/*definition?*/
				decl*;

module		:	MODULE id SEMI;
imports		:	IMPORT id (COMMA id)* COMMA? SEMI;
// which DEFINEs can be passed directly from the compiler, all others will be discarded
//defines	:	DEFINITION id (COMMA id)* COMMA? SEMI;


attribBlk	:	LBRACK	attrib (COMMA attrib)* COMMA? RBRACK;
attrib		:	attribId
				(	'=' idOrLit
				|	'(' idOrLit (COMMA idOrLit)* COMMA? ')'
				)?;
attribId	:	id | CONST | FALL | THROW | DEFAULT;


// DECLs

decl		:	defDecl		| attribBlk ( defDecl		| LCURLY decl*			RCURLY | COLON );
attrUsing	:	defUsing	| attribBlk ( defUsing		| LCURLY attrUsing*		RCURLY | COLON );
attrAlias	:	defAlias	| attribBlk ( defAlias		| LCURLY attrAlias*		RCURLY | COLON );
attrConvert	:	defConvert	| attribBlk ( defConvert	| LCURLY attrConvert*	RCURLY | COLON );
attrCtor	:	defCtor		| attribBlk ( defCtor		| LCURLY attrCtor*		RCURLY | COLON );
attrOp		:	defOp		| attribBlk ( defOp			| LCURLY attrOp*		RCURLY | COLON );
attrFunc	:	defFunc		| attribBlk ( defFunc		| LCURLY attrFunc*		RCURLY | COLON );
attrVar		:	defVar		| attribBlk ( defVar		| LCURLY attrVar*		RCURLY | COLON );

defDecl		:	declNamespace
			|	declUsing
			|	declAlias
			|	declAspect	// TODO
			|	declConcept	// TODO
			|	declEnum
			|	declStruct
			|	declConvert
			|	declCtor
			|	declDtor
			|	declOp
			|	declFunc
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
declOp		:	OPERATOR	( defOp			| LCURLY attrOp*		RCURLY	);
declFunc	:	kindOfFunc	( defFunc		| LCURLY attrFunc*		RCURLY	);
declVar		:	kindOfVar	( defVar		| LCURLY attrVar*		RCURLY	);

defNamespace:	id (SCOPE id)*
				(	SEMI
				|	COLON
				|	LCURLY	decl*		RCURLY);

defUsing	:	typespecsNested;
defAlias	:	id	tplParams?	ASSIGN	typespec;	// TODO: needs COMMA multiple support

defAspect	:	id	tplParams?;		// TODO
defConcept	:	id	tplParams?		// TODO
				(COLON	typespecsNested)?
					LCURLY	decl*		RCURLY;
defEnum		:	id
				(COLON		bases=typespecBasic)?	// TODO: enum inheritance
					LCURLY	idExprs			RCURLY;
defStruct	:	id	tplParams?
				(COLON		bases=typespecsNested)?
				(REQUIRES 	reqs=typespecsNested)?	// TODO
					LCURLY	decl*		RCURLY;

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
				// (REQUIRES	typespecsNested)?  ?
				funcBody;
defDtor		:	(LPAREN RPAREN)?
				funcBody;
defOp		:	(	kindOfPassing	ASSIGN				id?
				|	CONVERT			tplParams?	RARROW?	typespec // no param == no id
				|	STRING_LIT		defCoreFunc
				)
				(REQUIRES	typespecsNested)?
				funcBody;
defFunc		:	id			defCoreFunc
				(REQUIRES	typespecsNested)?
				funcBody;
defCoreFunc	:	tplParams?	funcTypeDef?
				(RARROW		typespec)?;
				//(REQUIRES	typespecsNested)?;

defVar		:	typedIdAcors;


// STMTs

stmt		:	defStmt		| attribBlk ( defStmt		| LCURLY stmt*			RCURLY );

defStmtNew	:	declUsing
			|	declAlias
			|	declVar
			|	stmtIf
			|	stmtSwitch
			|	stmtLoop
			|	stmtFor
			|	stmtWhile
			|	stmtDoWhile
			|	stmtTimes
			|	stmtTryCatch
			|	stmtReturn
			|	stmtReturnIf
			|	stmtThrow
			|	stmtBreak
			|	stmtAssign
			|	stmtAggregate
			|	stmtDefer
			|	stmtExpr
			|	stmtEmpty
			;

stmtIf		:	IF				condThen
				(ELSE IF		condThen)*	// helps with formatting properly and de-nesting the AST
				(ELSE			stmt)?;
stmtSwitch	:	SWITCH	LPAREN	cond=expr	RPAREN
				LCURLY	caseBlock+	defaultBlock?	RCURLY;

stmtLoop	:	LOOP			body=stmt;
stmtFor		:	FOR		LPAREN	init=stmt	// TODO: add syntax for( a : b )
								cond=expr?	SEMI
								iter=expr?	RPAREN	body=stmt	// assignment???
				(ELSE			els=stmt)?;
stmtWhile	:	WHILE	LPAREN	cond=expr	RPAREN	body=stmt
				(ELSE			els=stmt)?;
stmtDoWhile	:	DO				body=stmt
				WHILE	LPAREN	cond=expr	RPAREN;
stmtTimes	:	DO? 			count=expr	TIMES
				(name=id ((PLUS|MINUS) INTEGER_LIT)? )? body=stmt;

stmtTryCatch:	TRY						stmt
				(CATCH	funcTypeDef?	stmt)+; // funcTypeDef is wrong, but works for easy cases

stmtReturn	:	RETURN		expr?							SEMI;
stmtReturnIf:	DO RETURN	expr?	IF LPAREN expr RPAREN	SEMI; // same for throw and break, or make this a general contruct? replace "if" with "when"?
stmtThrow	:	THROW		expr							SEMI;
stmtBreak	:	BREAK		INTEGER_LIT?					SEMI;

stmtAssign	:	expr	(assignOP		expr)+	SEMI;
stmtAggregate:	expr	aggrAssignOP	expr	SEMI;

stmtDefer	:	DEFER	stmt;
stmtExpr	:	expr	SEMI;
stmtEmpty	:	SEMI;


// EXPRs

// ...


kindOfStruct:	v=(	STRUCT	|	CLASS	|	UNION	);
kindOfFunc	:	v=(	FUNC	|	PROC	|	METHOD	);
kindOfVar	:	v=(	VAR		|	FIELD	|	CONST	|	LET	);

kindOfPassing:	v=(	COPY	|	MOVE	|	FORWARD	);


// The great reordering Part 2, everything below still TODO


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
					'+='  | '-=' | '|=' | '^=' | '<<=' | '>>=' | '?=' );

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

typespecFunc	:	funcTypeDef? (RARROW typespec)?;

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
//cast:	nothing		= static_cast
//		?			= dynamic_cast
//		-			= const_cast
//		-const		= const_cast removing outer const
//		-volatile	= const_cast removing outer volatile
//		!			= std::bit_cast
//		!!			= reinterpret_cast
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
					|	MINUS CONST
					|	MINUS VOLATILE
					|	(QM|MINUS|EM|EM EM)? typespec 
					) RPAREN
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
			|	FUNC capture? tplParams? funcTypeDef?
				(RARROW typespec)?	funcBody	# LambdaExpr	// TODO
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
				(	COLON		stmt* (FALL SEMI)?
				|	LCURLY		stmt* (FALL SEMI)? RCURLY
				|	PHATRARROW	stmt);
defaultBlock:	(ELSE|DEFAULT)
				(	COLON		stmt*
				|	LCURLY		stmt* RCURLY
				|	PHATRARROW	stmt);

initList	:	COLON
				(	id		funcCall (COMMA id funcCall)*	COMMA?
				|	CTOR	funcCall						COMMA?
				);

// is just SEMI as well in stmt->defStmt
funcBody	:	PHATRARROW expr SEMI
			|	stmt;
accessorDef	:	attribBlk?	qual* v=( GET | REFGET | SET ) funcBody;
funcDef		:	id			tplParams?	funcTypeDef?
				(RARROW		typespec)?
				(REQUIRES	typespecsNested)?
				funcBody;
opDef		:	STRING_LIT	tplParams?	funcTypeDef?
				(RARROW		typespec)?
				(REQUIRES	typespecsNested)?
				funcBody;

// remove this?
condThen	:	LPAREN	expr	RPAREN	stmt
		//	|			expr			stmt // bench if this would hurt
		//	|			expr	COLON	stmt // try if this may work
			;

defStmt		:	SEMI								# EmptyStmt
			|	LCURLY	stmt*			RCURLY		# BlockStmt
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
				(ELSE		stmt)?					# IfStmt
			|	SWITCH	LPAREN cond=expr RPAREN	LCURLY
				caseBlock+ 	defaultBlock?	RCURLY	# SwitchStmt
			|	LOOP	body=stmt					# LoopStmt
			|	FOR LPAREN init=stmt	// TODO: add the syntax: for( a : b )
				cond=expr? SEMI
				iter=expr? RPAREN	body=stmt
				(ELSE				els=stmt)?		# ForStmt
			|	WHILE	LPAREN cond=expr RPAREN
						body=stmt
				(ELSE	els=stmt)?					# WhileStmt
			|	DO		body=stmt
				WHILE	LPAREN cond=expr RPAREN		# DoWhileStmt
			|	DO? count=expr TIMES
				(name=id ((PLUS|MINUS) INTEGER_LIT)? )?
						body=stmt					# TimesStmt
			|	TRY		stmt
			// funcTypeDef is wrong, but works for easy cases
				(CATCH	funcTypeDef?	stmt)+		# TryCatchStmt		// TODO
			|	DEFER	stmt						# DeferStmt			// TODO, scope_exit(,fail,success)
													// #include <experimental/scope>
													// std::experimental::scope_exit guard([]{ cout << "Exit!" << endl; });
			|	expr	(assignOP		expr)+ SEMI	# MultiAssignStmt
			|	expr	aggrAssignOP	expr SEMI	# AggrAssignStmt
			|	expr	SEMI						# ExpressionStmt
			;
