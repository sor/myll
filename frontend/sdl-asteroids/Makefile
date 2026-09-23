# SDL3 Asteroids sample for Myll.
# Run from this directory:
#   make
#   ./gen/asteroids

MYLL_ROOT ?= ../..
MYLL      := dotnet run --project $(MYLL_ROOT)/frontend -- -i '*.myll' -o gen

CXX       ?= g++
CXXFLAGS  ?= -std=c++23 -O2 -Wall -Wextra
SDL_CFLAGS = $(shell pkg-config --cflags sdl3)
SDL_LIBS   = $(shell pkg-config --libs sdl3)

.PHONY: all clean run

all: gen/asteroids

gen/.stamp: $(wildcard *.myll)
	@mkdir -p gen
	$(MYLL)
	@touch gen/.stamp

gen/asteroids: gen/.stamp
	$(CXX) $(CXXFLAGS) $(SDL_CFLAGS) gen/asteroids.cpp -o $@ $(SDL_LIBS)

run: gen/asteroids
	./gen/asteroids

clean:
	rm -rf gen
