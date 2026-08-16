# Build all Myll Unix utilities in this directory.
#
# Run from this directory:
#   make
#   make clean

MYLL_ROOT ?= ../../..
MYLL      := dotnet run --project $(MYLL_ROOT)/frontend -- -i '*.myll' -o gen

CXX      ?= g++
CXXFLAGS ?= -std=c++20

TOOLS    := $(basename $(wildcard *.myll))
BINS     := $(addprefix gen/,$(TOOLS))

.PHONY: all clean

all: $(BINS)

# Generate C++ sources for all .myll files in one Myll invocation.
gen/.stamp: $(wildcard *.myll)
	@mkdir -p gen
	$(MYLL)
	@touch gen/.stamp

# Compile each generated .cpp into its own binary.
$(BINS): gen/.stamp
	$(CXX) $(CXXFLAGS) gen/$(@F).cpp -o $@

clean:
	rm -rf gen
