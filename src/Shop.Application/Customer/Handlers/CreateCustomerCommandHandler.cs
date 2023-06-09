using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using MediatR;
using Shop.Application.Customer.Commands;
using Shop.Application.Customer.Responses;
using Shop.Core.Abstractions;
using Shop.Core.ValueObjects;
using Shop.Domain.Entities.CustomerAggregate.Repositories;

namespace Shop.Application.Customer.Handlers;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<CreatedCustomerResponse>>
{
    private readonly CreateCustomerCommandValidator _validator;
    private readonly ICustomerWriteOnlyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(
        CreateCustomerCommandValidator validator,
        ICustomerWriteOnlyRepository repository,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreatedCustomerResponse>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Validanto a requisição.
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return Result.Invalid(validationResult.AsErrors()); // Retorna o resultado com os erros da validação.

        // Instanciando o VO Email.
        var email = new Email(request.Email);

        // Verificiando se já existe um cliente com o endereço de e-mail.
        if (await _repository.ExistsByEmailAsync(email))
            return Result.Error("O endereço de e-mail informado já está sendo utilizado.");

        // Criando a instancia da entidade cliente.
        // Ao instanciar será criado o evento: "CustomerCreatedEvent"
        var customer = new Domain.Entities.CustomerAggregate.Customer(
            request.FirstName,
            request.LastName,
            request.Gender,
            email,
            request.DateOfBirth);

        // Adicionando a entidade no repositório.
        _repository.Add(customer);

        // Salvando as alterações no banco e disparando os eventos.
        await _unitOfWork.SaveChangesAsync();

        // Retornando o ID e a mensagem de sucesso.
        return Result.Success(new CreatedCustomerResponse(customer.Id), "Cadastrado com sucesso!");
    }
}