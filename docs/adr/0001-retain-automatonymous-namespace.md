# Retain the Automatonymous namespace over one Autonomata engine

Autonomata uses the package and product name `Autonomata` while retaining the public `Automatonymous` namespace for the approved core DSL, allowing standalone 5.1.3 consumers to migrate without changing namespace imports. A single internal Autonomata implementation backs that compatibility surface; parallel public engine hierarchies were rejected because they would duplicate concepts, fragment type identity, and increase maintenance cost.
